using ReReviewer.Application.ViewModels;

namespace ReReviewer.Application.Interfaces;

public interface IReviewerAppService
{
    public Task<List<ReviewerViewModel>> GetReviewersAsync();
    public Task<ReviewerViewModel> GetReviewerAsync(Guid reviewerId);
    public Task<bool> SaveReviewerAsync(ReviewerViewModel reviewer);
    public Task DeleteReviewerAsync(Guid reviewerId);

    public Task<ReviewerViewModel> ValidateReviewerAsync(Guid reviewerId, Guid userId);
}