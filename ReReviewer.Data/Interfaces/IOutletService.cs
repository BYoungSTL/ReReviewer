using ReReviewer.Data.Entities;
using ReReviewer.Data.Interfaces.Base;

namespace ReReviewer.Data.Interfaces;

public interface IOutletService : IBaseService<Outlet, Guid>
{
    public Task<Outlet> LinkReviewersAsync(Guid outletId, List<Reviewer> reviewers);
}