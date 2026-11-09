using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using ReReviewer.Data.CRUD.Base;
using ReReviewer.Data.Entities;
using ReReviewer.Data.Entities.Enums;
using ReReviewer.Data.Interfaces;

namespace ReReviewer.Data.CRUD;

public class ReviewerSuggestionService(ReReviewerDbContext context)
    : BaseService<ReviewerSuggestion, Guid>(context), IReviewerSuggestionService
{
    public async Task<ReviewerSuggestion> CreateAsync(Guid userId, string fullName, string? note, bool isSelf,
        IReadOnlyCollection<int> genreIds, CancellationToken cancellationToken = default)
    {
        var ids = genreIds.Distinct().ToList();
        if (ids.Count == 0)
            throw new ValidationException("At least one genre is required.");

        await ReviewerRequestRules.EnsureReviewerNameIsFreeAsync(Context, fullName, cancellationToken);
        if (await Pending().AnyAsync(s => s.FullName == fullName, cancellationToken))
            throw new ValidationException("This reviewer has already been suggested and is waiting for moderation.");
        if (isSelf)
            await ReviewerRequestRules.EnsureUserCanClaimAsync(Context, userId, cancellationToken);

        var genres = await Context.Genres.Where(g => ids.Contains(g.Id)).ToListAsync(cancellationToken);
        if (genres.Count != ids.Count)
            throw new KeyNotFoundException("One or more genres not found.");

        // Прокси, чтобы у возвращённой заявки лениво подгружался автор.
        var suggestion = Set.CreateProxy(s =>
        {
            s.UserId = userId;
            s.FullName = fullName;
            s.Note = note;
            s.IsSelf = isSelf;
            s.Status = RequestStatus.Pending;
        });
        foreach (var genre in genres)
            suggestion.Genres.Add(genre);

        return await AddAsync(suggestion, cancellationToken);
    }

    public async Task<IReadOnlyList<ReviewerSuggestion>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await Set
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt).ThenBy(s => s.Id)
            .ToListAsync(cancellationToken);

    // Первой идёт заявка, которая ждёт дольше всех.
    public async Task<IReadOnlyList<ReviewerSuggestion>> GetPendingAsync(int skip = 0, int take = 20, CancellationToken cancellationToken = default) =>
        await Pending()
            .OrderBy(s => s.CreatedAt).ThenBy(s => s.Id)
            .Skip(Math.Max(skip, 0))
            .Take(Math.Clamp(take, 1, ReviewerRequestRules.MaxPageSize))
            .ToListAsync(cancellationToken);

    public async Task<int> CountPendingAsync(CancellationToken cancellationToken = default) =>
        await Pending().CountAsync(cancellationToken);

    // Создаёт профиль с именем и жанрами из заявки, всё одним сохранением. Заявка «это я» привязывает профиль
    // к автору, если тот за это время не привязался к другому: тогда профиль создаётся без аккаунта.
    public async Task<ReviewerSuggestion> ApproveAsync(Guid suggestionId, CancellationToken cancellationToken = default)
    {
        var suggestion = await GetRequiredAsync(suggestionId, cancellationToken);
        ReviewerRequestRules.EnsurePending(suggestion.Status);
        await ReviewerRequestRules.EnsureReviewerNameIsFreeAsync(Context, suggestion.FullName, cancellationToken);

        var linkAuthor = suggestion.IsSelf
            && !await Context.Reviewers.AnyAsync(r => r.UserId == suggestion.UserId, cancellationToken);

        var reviewer = Context.CreateProxy<Reviewer>(r =>
        {
            r.FullName = suggestion.FullName;
            r.UserId = linkAuthor ? suggestion.UserId : null;
        });
        foreach (var genre in suggestion.Genres)
            reviewer.Genres.Add(genre);
        Context.Reviewers.Add(reviewer);

        suggestion.Status = RequestStatus.Approved;
        suggestion.DecidedAt = DateTime.UtcNow;
        suggestion.ReviewerId = reviewer.Id;

        await Context.SaveChangesAsync(cancellationToken);

        return suggestion;
    }

    public async Task<ReviewerSuggestion> RejectAsync(Guid suggestionId, CancellationToken cancellationToken = default)
    {
        var suggestion = await GetRequiredAsync(suggestionId, cancellationToken);
        ReviewerRequestRules.EnsurePending(suggestion.Status);

        suggestion.Status = RequestStatus.Rejected;
        suggestion.DecidedAt = DateTime.UtcNow;
        await Context.SaveChangesAsync(cancellationToken);

        return suggestion;
    }

    private IQueryable<ReviewerSuggestion> Pending() =>
        Set.Where(s => s.Status == RequestStatus.Pending);
}
