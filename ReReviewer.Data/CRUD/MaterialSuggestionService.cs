using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using ReReviewer.Data.CRUD.Base;
using ReReviewer.Data.Entities;
using ReReviewer.Data.Entities.Enums;
using ReReviewer.Data.Interfaces;

namespace ReReviewer.Data.CRUD;

// Заявки на новые материалы обзорщиков. Ссылка материала уникальна (индекс Materials.Url), поэтому заявку на уже
// добавленный или уже предложенный материал не принять; при одобрении это проверяется ещё раз.
public class MaterialSuggestionService(ReReviewerDbContext context)
    : BaseService<MaterialSuggestion, Guid>(context), IMaterialSuggestionService
{
    public async Task<MaterialSuggestion> CreateAsync(Guid userId, Guid reviewerId, Guid gameId, string title, string url,
        MaterialFormat format, DateOnly? publishedAt, CancellationToken cancellationToken = default)
    {
        if (!await Context.Reviewers.AnyAsync(r => r.Id == reviewerId, cancellationToken))
            throw new KeyNotFoundException("Reviewer not found.");
        if (!await Context.Games.AnyAsync(g => g.Id == gameId, cancellationToken))
            throw new KeyNotFoundException("Game not found.");

        await EnsureUrlIsFreeAsync(url, cancellationToken);
        // Ссылку проверяем и среди заявок из отзывов, которые ждут решения вместе с отзывом.
        if (await Set.AnyAsync(s => s.Status == RequestStatus.Pending && s.Url == url, cancellationToken))
            throw new ValidationException("This material has already been suggested and is waiting for moderation.");

        // Прокси, чтобы у возвращённой заявки лениво подгружались обзорщик, игра и автор.
        var suggestion = Set.CreateProxy(s =>
        {
            s.UserId = userId;
            s.ReviewerId = reviewerId;
            s.GameId = gameId;
            s.Title = title;
            s.Url = url;
            s.Format = format;
            s.PublishedAt = publishedAt;
            s.Status = RequestStatus.Pending;
        });

        return await AddAsync(suggestion, cancellationToken);
    }

    // Заявки из отзывов видны в самих отзывах, поэтому здесь только отдельные.
    public async Task<IReadOnlyList<MaterialSuggestion>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await Standalone()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt).ThenBy(s => s.Id)
            .ToListAsync(cancellationToken);

    // Первой идёт заявка, которая ждёт дольше всех.
    public async Task<IReadOnlyList<MaterialSuggestion>> GetPendingAsync(int skip = 0, int take = 20, CancellationToken cancellationToken = default) =>
        await Pending()
            .OrderBy(s => s.CreatedAt).ThenBy(s => s.Id)
            .Skip(Math.Max(skip, 0))
            .Take(Math.Clamp(take, 1, ReviewerRequestRules.MaxPageSize))
            .ToListAsync(cancellationToken);

    public async Task<int> CountPendingAsync(CancellationToken cancellationToken = default) =>
        await Pending().CountAsync(cancellationToken);

    // Материал создаётся и заявка закрывается одним сохранением.
    public async Task<MaterialSuggestion> ApproveAsync(Guid suggestionId, CancellationToken cancellationToken = default)
    {
        var suggestion = await GetStandaloneAsync(suggestionId, cancellationToken);
        ReviewerRequestRules.EnsurePending(suggestion.Status);
        // Пока заявка ждала, материал с этой ссылкой мог добавить модератор или одобрить другую заявку.
        await EnsureUrlIsFreeAsync(suggestion.Url, cancellationToken);

        var material = Context.CreateProxy<Material>(m =>
        {
            m.ReviewerId = suggestion.ReviewerId;
            m.GameId = suggestion.GameId;
            m.Title = suggestion.Title;
            m.Url = suggestion.Url;
            m.Format = suggestion.Format;
            m.PublishedAt = suggestion.PublishedAt;
        });
        Context.Materials.Add(material);

        suggestion.Status = RequestStatus.Approved;
        suggestion.DecidedAt = DateTime.UtcNow;
        suggestion.MaterialId = material.Id;

        await Context.SaveChangesAsync(cancellationToken);

        return suggestion;
    }

    public async Task<MaterialSuggestion> RejectAsync(Guid suggestionId, CancellationToken cancellationToken = default)
    {
        var suggestion = await GetStandaloneAsync(suggestionId, cancellationToken);
        ReviewerRequestRules.EnsurePending(suggestion.Status);

        suggestion.Status = RequestStatus.Rejected;
        suggestion.DecidedAt = DateTime.UtcNow;
        await Context.SaveChangesAsync(cancellationToken);

        return suggestion;
    }

    // Заявки из отзывов модерируются вместе с отзывом (ReviewService.SetStatusAsync), поэтому в очередь не попадают.
    private IQueryable<MaterialSuggestion> Standalone() =>
        Set.Where(s => s.ReviewId == null);

    private IQueryable<MaterialSuggestion> Pending() =>
        Standalone().Where(s => s.Status == RequestStatus.Pending);

    private async Task<MaterialSuggestion> GetStandaloneAsync(Guid suggestionId, CancellationToken cancellationToken)
    {
        var suggestion = await GetRequiredAsync(suggestionId, cancellationToken);
        if (suggestion.ReviewId is not null)
            throw new ValidationException("This material is part of a review and is moderated together with it.");

        return suggestion;
    }

    private async Task EnsureUrlIsFreeAsync(string url, CancellationToken cancellationToken)
    {
        if (await Context.Materials.AnyAsync(m => m.Url == url, cancellationToken))
            throw new ValidationException("A material with this link already exists.");
    }
}
