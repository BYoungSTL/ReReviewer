using ReReviewer.Application.ViewModels;

namespace ReReviewer.Application.Interfaces;

// Заявки, которые рассматривают модераторы: новые обзорщики и привязка профиля к аккаунту.
public interface IReviewerRequestAppService
{
    public Task<ReviewerSuggestionViewModel> SuggestReviewerAsync(Guid userId, ReviewerSuggestionViewModel suggestion);
    public Task<List<ReviewerSuggestionViewModel>> GetUserSuggestionsAsync(Guid userId);
    public Task<PagedViewModel<ReviewerSuggestionViewModel>> GetSuggestionQueueAsync(int page = 1, int pageSize = 20);
    public Task ModerateSuggestionAsync(Guid suggestionId, bool approve);

    public Task<ReviewerClaimViewModel> ClaimReviewerAsync(Guid userId, ReviewerClaimViewModel claim);
    public Task<List<ReviewerClaimViewModel>> GetUserClaimsAsync(Guid userId);
    public Task<PagedViewModel<ReviewerClaimViewModel>> GetClaimQueueAsync(int page = 1, int pageSize = 20);
    public Task ModerateClaimAsync(Guid claimId, bool approve);
}
