using System.ComponentModel.DataAnnotations;
using ReReviewer.Application.Interfaces;
using ReReviewer.Application.Services.Base;
using ReReviewer.Application.ViewModels;
using ReReviewer.Data.Entities;
using ReReviewer.Data.Interfaces;

namespace ReReviewer.Application.Services;

// Здесь — проверка ввода и сборка ViewModel. Правила заявок (имя свободно, пользователь ещё не привязан,
// одна заявка на себя за раз) проверяют CRUD-сервисы слоя Data: им нужна база.
public class ReviewerRequestAppService(IReviewerSuggestionService suggestionService, IReviewerClaimService claimService)
    : IReviewerRequestAppService
{
    private const int MaxNameLength = 250;
    private const int MaxNoteLength = 1000;
    private const int MaxPageSize = 100;

    #region Suggestions

    public async Task<ReviewerSuggestionViewModel> SuggestReviewerAsync(Guid userId, ReviewerSuggestionViewModel suggestion)
    {
        var fullName = MaxLength(InputValidation.RequiredText(suggestion.FullName, "Reviewer name"), MaxNameLength, "Reviewer name");
        var note = InputValidation.OptionalText(suggestion.Note);
        if (note is not null)
            MaxLength(note, MaxNoteLength, "Note");
        var genreIds = InputValidation.RequiredIds(suggestion.Genres, g => g.Id, "Genre");

        var saved = await suggestionService.CreateAsync(userId, fullName, note, suggestion.IsSelf, genreIds);

        return ToViewModel(saved);
    }

    public async Task<List<ReviewerSuggestionViewModel>> GetUserSuggestionsAsync(Guid userId)
    {
        var suggestions = await suggestionService.GetByUserAsync(userId);

        return suggestions.Select(ToViewModel).ToList();
    }

    public async Task<PagedViewModel<ReviewerSuggestionViewModel>> GetSuggestionQueueAsync(int page = 1, int pageSize = 20)
    {
        (page, pageSize) = NormalizePage(page, pageSize);

        var suggestions = await suggestionService.GetPendingAsync((page - 1) * pageSize, pageSize);
        var totalCount = await suggestionService.CountPendingAsync();

        return ToPage(suggestions.Select(ToViewModel), page, pageSize, totalCount);
    }

    public async Task ModerateSuggestionAsync(Guid suggestionId, bool approve)
    {
        if (approve)
            await suggestionService.ApproveAsync(suggestionId);
        else
            await suggestionService.RejectAsync(suggestionId);
    }

    #endregion

    #region Claims

    public async Task<ReviewerClaimViewModel> ClaimReviewerAsync(Guid userId, ReviewerClaimViewModel claim)
    {
        var note = MaxLength(InputValidation.RequiredText(claim.Note, "Proof"), MaxNoteLength, "Proof");

        var saved = await claimService.CreateAsync(userId, claim.ReviewerId, note);

        return ToViewModel(saved);
    }

    public async Task<List<ReviewerClaimViewModel>> GetUserClaimsAsync(Guid userId)
    {
        var claims = await claimService.GetByUserAsync(userId);

        return claims.Select(ToViewModel).ToList();
    }

    public async Task<PagedViewModel<ReviewerClaimViewModel>> GetClaimQueueAsync(int page = 1, int pageSize = 20)
    {
        (page, pageSize) = NormalizePage(page, pageSize);

        var claims = await claimService.GetPendingAsync((page - 1) * pageSize, pageSize);
        var totalCount = await claimService.CountPendingAsync();

        return ToPage(claims.Select(ToViewModel), page, pageSize, totalCount);
    }

    public async Task ModerateClaimAsync(Guid claimId, bool approve)
    {
        if (approve)
            await claimService.ApproveAsync(claimId);
        else
            await claimService.RejectAsync(claimId);
    }

    #endregion

    #region Helpers

    private static string MaxLength(string value, int maxLength, string fieldName) =>
        value.Length > maxLength
            ? throw new ValidationException($"{fieldName} must be at most {maxLength} characters long.")
            : value;

    private static ReviewerSuggestionViewModel ToViewModel(ReviewerSuggestion suggestion) =>
        new()
        {
            Id = suggestion.Id,
            FullName = suggestion.FullName,
            Note = suggestion.Note,
            IsSelf = suggestion.IsSelf,
            Genres = suggestion.Genres
                .OrderBy(g => g.Title)
                .Select(g => new GenreViewModel { Id = g.Id, Title = g.Title })
                .ToList(),
            UserId = suggestion.UserId,
            UserName = suggestion.User.Name,
            Status = suggestion.Status.ToString(),
            CreatedAt = suggestion.CreatedAt,
            DecidedAt = suggestion.DecidedAt,
            ReviewerId = suggestion.ReviewerId
        };

    private static ReviewerClaimViewModel ToViewModel(ReviewerClaim claim) =>
        new()
        {
            Id = claim.Id,
            ReviewerId = claim.ReviewerId,
            Note = claim.Note,
            ReviewerName = claim.Reviewer.FullName,
            UserId = claim.UserId,
            UserName = claim.User.Name,
            Status = claim.Status.ToString(),
            CreatedAt = claim.CreatedAt,
            DecidedAt = claim.DecidedAt
        };

    private static (int Page, int PageSize) NormalizePage(int page, int pageSize) =>
        (Math.Max(page, 1), Math.Clamp(pageSize, 1, MaxPageSize));

    private static PagedViewModel<T> ToPage<T>(IEnumerable<T> items, int page, int pageSize, int totalCount) =>
        new()
        {
            Items = items.ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

    #endregion
}
