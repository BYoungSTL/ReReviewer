using ReReviewer.Data.Entities;
using ReReviewer.Data.Entities.Enums;
using ReReviewer.Data.Interfaces.Base;
using ReReviewer.Data.Models;

namespace ReReviewer.Data.Interfaces;

public interface IReviewService : IBaseService<Review, Guid>
{
    public Task<Review> CreateAsync(Guid userId, Guid reviewerId, ReviewInput input, CancellationToken cancellationToken = default);
    public Task<Review> EditAsync(Guid reviewId, Guid userId, ReviewInput input, CancellationToken cancellationToken = default);
    public Task DeleteOwnAsync(Guid reviewId, Guid userId, CancellationToken cancellationToken = default);
    public Task SetStatusAsync(Guid reviewId, ReviewStatus status, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<Review>> GetPublishedByReviewerAsync(Guid reviewerId, int skip = 0, int take = 20, CancellationToken cancellationToken = default);
    public Task<int> CountPublishedByReviewerAsync(Guid reviewerId, CancellationToken cancellationToken = default);
    public Task<IReadOnlyList<Review>> GetPendingAsync(int skip = 0, int take = 20, CancellationToken cancellationToken = default);
    public Task<int> CountPendingAsync(CancellationToken cancellationToken = default);
    public Task<IReadOnlyList<Review>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    public Task<ReviewerRating> GetReviewerRatingAsync(Guid reviewerId, CancellationToken cancellationToken = default);

    // Без reviewerIds — по всем обзорщикам. Обзорщиков без опубликованных отзывов в словаре нет.
    public Task<IReadOnlyDictionary<Guid, ReviewerScoreSummary>> GetPublishedSummariesAsync(
        IReadOnlyCollection<Guid>? reviewerIds = null, CancellationToken cancellationToken = default);
}