using ReReviewer.Application.ViewModels;

namespace ReReviewer.Application.Interfaces;

public interface IOutletAppService
{
    public Task<List<OutletViewModel>> GetOutletsAsync();
    public Task<OutletViewModel> GetOutletAsync(Guid outletId);
    public Task<bool> SaveOutletAsync(OutletViewModel outlet);
    public Task DeleteOutletAsync(Guid outletId);

    public Task<OutletViewModel> LinkReviewersAsync(Guid outletId, List<Guid> reviewerIds);
}