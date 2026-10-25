using ReReviewer.Application.ViewModels;

namespace ReReviewer.Application.Interfaces;

public interface IReviewAppService
{
    public Task<List<ReviewViewModel>> GetUserReviewsAsync(Guid userId);
    public Task<ReviewViewModel> GetReviewAsync(Guid reviewId);
    public Task<bool> SaveReviewAsync(ReviewViewModel review);

    public Task<PagedViewModel<ReviewViewModel>> GetReviewerReviewsAsync(Guid reviewerId, int page = 1, int pageSize = 20);
    public Task<ReviewerRatingViewModel> GetReviewerRatingAsync(Guid reviewerId);
    public Task DeleteReviewAsync(Guid reviewId, Guid userId);

    public Task<PagedViewModel<ReviewViewModel>> GetModerationQueueAsync(int page = 1, int pageSize = 20);
    public Task ModerateReviewAsync(Guid reviewId, bool approve);
}