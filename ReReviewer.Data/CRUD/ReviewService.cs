using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using ReReviewer.Data.CRUD.Base;
using ReReviewer.Data.Entities;
using ReReviewer.Data.Entities.Enums;
using ReReviewer.Data.Interfaces;
using ReReviewer.Data.Models;

namespace ReReviewer.Data.CRUD;

// Ошибки: KeyNotFoundException — не найдена запись, ValidationException — некорректные данные,
// UnauthorizedAccessException — попытка изменить чужой отзыв.
public class ReviewService(ReReviewerDbContext context) : BaseService<Review, Guid>(context), IReviewService
{
    private const byte MinScore = 1;
    private const byte MaxScore = 10;
    private const int MaxPageSize = 100;

    #region Create / Edit / Delete

    public async Task<Review> CreateAsync(Guid userId, Guid reviewerId, ReviewInput input, CancellationToken cancellationToken = default)
    {
        // Новые записи создаются как прокси, чтобы у возвращённого отзыва лениво подгружались
        // Reviewer, User и Criterion; у объекта из обычного new этого не будет.
        var review = Set.CreateProxy(r =>
        {
            r.UserId = userId;
            r.ReviewerId = reviewerId;
            r.Text = input.Text;
            r.Score = input.Score;
            r.Status = ReviewStatus.Pending;
        });

        foreach (var material in await LoadMaterialsAsync(input.MaterialIds, cancellationToken))
            review.Materials.Add(material);

        foreach (var (criterionId, score) in input.CriterionScores)
            review.CriterionScores.Add(CreateScore(criterionId, score));

        return await AddAsync(review, cancellationToken);
    }

    // Materials и CriterionScores подгружаются лениво при первом обращении в Sync-методах.
    public async Task<Review> EditAsync(Guid reviewId, Guid userId, ReviewInput input, CancellationToken cancellationToken = default)
    {
        var review = await GetRequiredAsync(reviewId, cancellationToken);

        if (review.UserId != userId)
            throw new UnauthorizedAccessException("Only the author can edit this review.");

        review.Text = input.Text;
        review.Score = input.Score;
        review.UpdatedAt = DateTime.UtcNow;
        // Изменённый отзыв снова проходит модерацию.
        review.Status = ReviewStatus.Pending;

        await SyncMaterialsAsync(review, input.MaterialIds, cancellationToken);
        SyncCriterionScores(review, input.CriterionScores);

        await UpdateAsync(review, cancellationToken);

        return review;
    }

    public async Task DeleteOwnAsync(Guid reviewId, Guid userId, CancellationToken cancellationToken = default)
    {
        var review = await GetRequiredAsync(reviewId, cancellationToken);

        if (review.UserId != userId)
            throw new UnauthorizedAccessException("Only the author can delete this review.");

        Set.Remove(review);
        await Context.SaveChangesAsync(cancellationToken);
    }

    public async Task SetStatusAsync(Guid reviewId, ReviewStatus status, CancellationToken cancellationToken = default)
    {
        var review = await GetRequiredAsync(reviewId, cancellationToken);

        review.Status = status;
        await Context.SaveChangesAsync(cancellationToken);
    }

    // Базовые AddAsync/UpdateAsync тоже доступны через IReviewService, поэтому проверки стоят здесь,
    // а не только в CreateAsync/EditAsync.
    public override async Task<Review> AddAsync(Review entity, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(entity, cancellationToken);
        return await base.AddAsync(entity, cancellationToken);
    }

    public override async Task UpdateAsync(Review entity, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(entity, cancellationToken);
        await base.UpdateAsync(entity, cancellationToken);
    }

    #endregion

    #region Queries

    // Навигации (Materials, CriterionScores, Reviewer) подгружаются лениво: отдельный запрос
    // на каждую навигацию каждого отзыва.
    public async Task<IReadOnlyList<Review>> GetPublishedByReviewerAsync(Guid reviewerId, int skip = 0, int take = 20, CancellationToken cancellationToken = default) =>
        await PublishedByReviewer(reviewerId)
            .AsNoTracking()
            .OrderByDescending(r => r.CreatedAt).ThenBy(r => r.Id)
            .Skip(Math.Max(skip, 0))
            .Take(Math.Clamp(take, 1, MaxPageSize))
            .ToListAsync(cancellationToken);

    public async Task<int> CountPublishedByReviewerAsync(Guid reviewerId, CancellationToken cancellationToken = default) =>
        await PublishedByReviewer(reviewerId).CountAsync(cancellationToken);

    // Очередь модерации: первым идёт отзыв, который ждёт дольше всех (с момента создания или последней правки).
    public async Task<IReadOnlyList<Review>> GetPendingAsync(int skip = 0, int take = 20, CancellationToken cancellationToken = default) =>
        await Pending()
            .AsNoTracking()
            .OrderBy(r => r.UpdatedAt ?? r.CreatedAt).ThenBy(r => r.Id)
            .Skip(Math.Max(skip, 0))
            .Take(Math.Clamp(take, 1, MaxPageSize))
            .ToListAsync(cancellationToken);

    public async Task<int> CountPendingAsync(CancellationToken cancellationToken = default) =>
        await Pending().CountAsync(cancellationToken);

    public async Task<IReadOnlyList<Review>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await Set
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt).ThenBy(r => r.Id)
            .ToListAsync(cancellationToken);

    // В рейтинг идут только опубликованные отзывы.
    public async Task<ReviewerRating> GetReviewerRatingAsync(Guid reviewerId, CancellationToken cancellationToken = default)
    {
        if (!await Context.Reviewers.AnyAsync(r => r.Id == reviewerId, cancellationToken))
            throw new KeyNotFoundException("Reviewer not found.");

        var published = PublishedByReviewer(reviewerId);

        var reviewCount = await published.CountAsync(cancellationToken);
        double? averageScore = reviewCount == 0
            ? null
            : await published.AverageAsync(r => (double)r.Score, cancellationToken);

        var criteria = await Context.ReviewCriterionScores
            .Where(s => s.Review.ReviewerId == reviewerId && s.Review.Status == ReviewStatus.Published)
            .GroupBy(s => new { s.CriterionId, s.Criterion.Title })
            .OrderBy(g => g.Key.Title)
            .Select(g => new CriterionRating(g.Key.CriterionId, g.Key.Title, g.Average(s => (double)s.Score), g.Count()))
            .ToListAsync(cancellationToken);

        return new ReviewerRating(reviewerId, reviewCount, averageScore, criteria);
    }

    #endregion

    #region Helpers

    private IQueryable<Review> PublishedByReviewer(Guid reviewerId) =>
        Set.Where(r => r.ReviewerId == reviewerId && r.Status == ReviewStatus.Published);

    private IQueryable<Review> Pending() =>
        Set.Where(r => r.Status == ReviewStatus.Pending);

    private async Task ValidateAsync(Review review, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(review.Text))
            throw new ValidationException("Review text must not be empty.");

        if (review.Score is < MinScore or > MaxScore)
            throw new ValidationException($"Score must be between {MinScore} and {MaxScore}.");

        if (review.CriterionScores.Any(s => s.Score is < MinScore or > MaxScore))
            throw new ValidationException($"Criterion score must be between {MinScore} and {MaxScore}.");

        var criterionIds = review.CriterionScores.Select(s => s.CriterionId).ToList();
        if (criterionIds.Distinct().Count() != criterionIds.Count)
            throw new ValidationException("Each criterion can be scored only once.");

        if (await Context.Criteria.CountAsync(c => criterionIds.Contains(c.Id), cancellationToken) != criterionIds.Count)
            throw new KeyNotFoundException("One or more criteria not found.");

        if (!await Context.Reviewers.AnyAsync(r => r.Id == review.ReviewerId, cancellationToken))
            throw new KeyNotFoundException("Reviewer not found.");

        if (review.Materials.Any(m => m.ReviewerId != review.ReviewerId))
            throw new ValidationException("A review can only reference materials of the reviewer it is about.");

        var alreadyReviewed = await Set.AnyAsync(
            r => r.UserId == review.UserId && r.ReviewerId == review.ReviewerId && r.Id != review.Id,
            cancellationToken);
        if (alreadyReviewed)
            throw new ValidationException("You have already reviewed this reviewer. Edit your existing review instead.");
    }

    private async Task<List<Material>> LoadMaterialsAsync(IReadOnlyCollection<Guid> materialIds, CancellationToken cancellationToken)
    {
        var ids = materialIds.Distinct().ToList();
        var materials = await Context.Materials
            .Where(m => ids.Contains(m.Id))
            .ToListAsync(cancellationToken);

        if (materials.Count != ids.Count)
            throw new KeyNotFoundException("One or more materials not found.");

        return materials;
    }

    // Коллекции синхронизируются точечно, а не через Clear() и повторное добавление,
    // чтобы не удалять и не вставлять заново строки, которые не изменились.
    private async Task SyncMaterialsAsync(Review review, IReadOnlyCollection<Guid> materialIds, CancellationToken cancellationToken)
    {
        var materials = await LoadMaterialsAsync(materialIds, cancellationToken);

        foreach (var removed in review.Materials.Where(m => !materialIds.Contains(m.Id)).ToList())
            review.Materials.Remove(removed);

        foreach (var added in materials.Where(m => review.Materials.All(x => x.Id != m.Id)).ToList())
            review.Materials.Add(added);
    }

    private void SyncCriterionScores(Review review, IReadOnlyDictionary<int, byte> criterionScores)
    {
        foreach (var removed in review.CriterionScores.Where(s => !criterionScores.ContainsKey(s.CriterionId)).ToList())
            review.CriterionScores.Remove(removed);

        foreach (var (criterionId, score) in criterionScores)
        {
            var existing = review.CriterionScores.FirstOrDefault(s => s.CriterionId == criterionId);
            if (existing is not null)
                existing.Score = score;
            else
                review.CriterionScores.Add(CreateScore(criterionId, score));
        }
    }

    private ReviewCriterionScore CreateScore(int criterionId, byte score) =>
        Context.CreateProxy<ReviewCriterionScore>(s =>
        {
            s.CriterionId = criterionId;
            s.Score = score;
        });

    #endregion
}