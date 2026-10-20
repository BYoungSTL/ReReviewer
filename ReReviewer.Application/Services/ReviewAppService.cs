using System.ComponentModel.DataAnnotations;
using ReReviewer.Application.Interfaces;
using ReReviewer.Application.ViewModels;
using ReReviewer.Data.Entities;
using ReReviewer.Data.Entities.Enums;
using ReReviewer.Data.Interfaces;
using ReReviewer.Data.Models;

namespace ReReviewer.Application.Services;

// К базе напрямую не обращается: все данные берутся и сохраняются через CRUD-сервисы слоя Data.
// Автор, обзорщик, материалы и критерии отзыва подгружаются лениво при сборке ViewModel.
// Ошибки пробрасываются как есть: KeyNotFoundException, ValidationException, UnauthorizedAccessException.
public class ReviewAppService(IReviewService reviewService) : IReviewAppService
{
    private const int MaxPageSize = 100;

    #region Reviews

    public async Task<List<ReviewViewModel>> GetUserReviewsAsync(Guid userId)
    {
        var reviews = await reviewService.GetByUserAsync(userId);

        return reviews.Select(ToViewModel).ToList();
    }

    public async Task<ReviewViewModel> GetReviewAsync(Guid reviewId)
    {
        var review = await reviewService.GetByIdAsync(reviewId)
            ?? throw new KeyNotFoundException("Review not found.");

        return ToViewModel(review);
    }

    // Без Id создаёт отзыв, с Id — правит существующий (только автор; обзорщик у отзыва не меняется).
    // После сохранения в переданную модель записываются Id, статус и даты.
    public async Task<bool> SaveReviewAsync(ReviewViewModel review)
    {
        var input = ToInput(review);

        var saved = review.Id is { } id && id != Guid.Empty
            ? await reviewService.EditAsync(id, review.UserId, input)
            : await reviewService.CreateAsync(review.UserId, review.ReviewerId, input);

        review.Id = saved.Id;
        review.ReviewerId = saved.ReviewerId;
        review.Status = saved.Status.ToString();
        review.CreatedAt = saved.CreatedAt;
        review.UpdatedAt = saved.UpdatedAt;

        return true;
    }

    public async Task DeleteReviewAsync(Guid reviewId, Guid userId) =>
        await reviewService.DeleteOwnAsync(reviewId, userId);

    #endregion

    #region Reviewer page

    // Только опубликованные отзывы, новые сверху.
    public async Task<PagedViewModel<ReviewViewModel>> GetReviewerReviewsAsync(Guid reviewerId, int page = 1, int pageSize = 20)
    {
        (page, pageSize) = NormalizePage(page, pageSize);

        var reviews = await reviewService.GetPublishedByReviewerAsync(reviewerId, (page - 1) * pageSize, pageSize);
        var totalCount = await reviewService.CountPublishedByReviewerAsync(reviewerId);

        return ToPage(reviews, page, pageSize, totalCount);
    }

    public async Task<ReviewerRatingViewModel> GetReviewerRatingAsync(Guid reviewerId)
    {
        var rating = await reviewService.GetReviewerRatingAsync(reviewerId);

        return new ReviewerRatingViewModel
        {
            ReviewerId = rating.ReviewerId,
            ReviewCount = rating.ReviewCount,
            AverageScore = rating.AverageScore,
            Criteria = rating.Criteria
                .Select(c => new CriterionRatingViewModel
                {
                    CriterionId = c.CriterionId,
                    Title = c.Title,
                    AverageScore = c.AverageScore,
                    ScoreCount = c.ScoreCount
                })
                .ToList()
        };
    }

    #endregion

    #region Moderation

    // Отзывы на модерации, первым — тот, что ждёт дольше всех.
    public async Task<PagedViewModel<ReviewViewModel>> GetModerationQueueAsync(int page = 1, int pageSize = 20)
    {
        (page, pageSize) = NormalizePage(page, pageSize);

        var reviews = await reviewService.GetPendingAsync((page - 1) * pageSize, pageSize);
        var totalCount = await reviewService.CountPendingAsync();

        return ToPage(reviews, page, pageSize, totalCount);
    }

    public async Task ModerateReviewAsync(Guid reviewId, bool approve) =>
        await reviewService.SetStatusAsync(reviewId, approve ? ReviewStatus.Published : ReviewStatus.Rejected);

    #endregion

    #region Helpers

    private static ReviewViewModel ToViewModel(Review review) =>
        new()
        {
            Id = review.Id,
            UserId = review.UserId,
            ReviewerId = review.ReviewerId,
            Text = review.Text,
            Score = review.Score,
            Materials = review.Materials
                .OrderBy(m => m.Title)
                .Select(m => new ReviewMaterialViewModel
                {
                    Id = m.Id,
                    Title = m.Title,
                    Url = m.Url,
                    Format = m.Format.ToString()
                })
                .ToList(),
            CriterionScores = review.CriterionScores
                .OrderBy(s => s.Criterion.Title)
                .Select(s => new ReviewCriterionScoreViewModel
                {
                    CriterionId = s.CriterionId,
                    Score = s.Score,
                    CriterionTitle = s.Criterion.Title
                })
                .ToList(),
            UserName = review.User.Name,
            ReviewerName = review.Reviewer.FullName,
            Status = review.Status.ToString(),
            CreatedAt = review.CreatedAt,
            UpdatedAt = review.UpdatedAt
        };

    private static ReviewInput ToInput(ReviewViewModel review)
    {
        var criterionIds = review.CriterionScores.Select(s => s.CriterionId).ToList();
        if (criterionIds.Distinct().Count() != criterionIds.Count)
            throw new ValidationException("Each criterion can be scored only once.");

        return new ReviewInput(
            review.Text,
            review.Score,
            review.Materials.Select(m => m.Id).ToList(),
            review.CriterionScores.ToDictionary(s => s.CriterionId, s => s.Score));
    }

    private static (int Page, int PageSize) NormalizePage(int page, int pageSize) =>
        (Math.Max(page, 1), Math.Clamp(pageSize, 1, MaxPageSize));

    private static PagedViewModel<ReviewViewModel> ToPage(IEnumerable<Review> reviews, int page, int pageSize, int totalCount) =>
        new()
        {
            Items = reviews.Select(ToViewModel).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

    #endregion
}