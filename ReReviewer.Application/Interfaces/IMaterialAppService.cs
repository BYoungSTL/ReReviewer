using ReReviewer.Application.ViewModels;

namespace ReReviewer.Application.Interfaces;

public interface IMaterialAppService
{
    public Task<MaterialViewModel> GetMaterialAsync(Guid materialId);
    public Task<List<MaterialViewModel>> GetReviewerMaterialsAsync(Guid reviewerId);
    public Task<List<MaterialViewModel>> GetGameMaterialsAsync(Guid gameId);
    public Task<bool> SaveMaterialAsync(MaterialViewModel material);
    public Task DeleteMaterialAsync(Guid materialId);

    public Task<List<MaterialViewModel>> LinkMaterialsAsync(Guid reviewerId, List<MaterialViewModel> materials);
}