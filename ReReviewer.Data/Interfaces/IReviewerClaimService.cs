using ReReviewer.Data.Entities;
using ReReviewer.Data.Interfaces.Base;

namespace ReReviewer.Data.Interfaces;

public interface IReviewerClaimService : IBaseService<ReviewerClaim, Guid>
{
    public Task<ReviewerClaim> CreateAsync(Guid userId, Guid reviewerId, string note, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<ReviewerClaim>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    public Task<IReadOnlyList<ReviewerClaim>> GetPendingAsync(int skip = 0, int take = 20, CancellationToken cancellationToken = default);
    public Task<int> CountPendingAsync(CancellationToken cancellationToken = default);

    public Task<ReviewerClaim> ApproveAsync(Guid claimId, CancellationToken cancellationToken = default);
    public Task<ReviewerClaim> RejectAsync(Guid claimId, CancellationToken cancellationToken = default);
}
