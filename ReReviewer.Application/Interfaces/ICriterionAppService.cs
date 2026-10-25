using ReReviewer.Application.ViewModels;

namespace ReReviewer.Application.Interfaces;

public interface ICriterionAppService
{
    public Task<List<CriterionViewModel>> GetCriteriaAsync();
    public Task<CriterionViewModel> GetCriterionAsync(int criterionId);
    public Task<bool> SaveCriterionAsync(CriterionViewModel criterion);
    public Task DeleteCriterionAsync(int criterionId);
}