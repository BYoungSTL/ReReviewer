using ReReviewer.Data.Entities;
using ReReviewer.Data.Interfaces.Base;

namespace ReReviewer.Data.Interfaces;

public interface IReviewerService : IBaseService<Reviewer, Guid>
{
    public Task<Reviewer> ValidateReviewerAsync(Reviewer reviewer, Guid userId);
    public Task<Reviewer> LinkMaterialsAsync(Reviewer reviewer, List<Material> materials);
}