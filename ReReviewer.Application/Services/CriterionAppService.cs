using ReReviewer.Application.Interfaces;
using ReReviewer.Application.Services.Base;
using ReReviewer.Application.ViewModels;
using ReReviewer.Data.Entities;
using ReReviewer.Data.Interfaces;

namespace ReReviewer.Application.Services;

// Новый критерий сразу становится обязательным для новых отзывов и правок. В уже написанных отзывах его оценки нет:
// на странице обзорщика он будет «без оценок», пока его не оценят.
public class CriterionAppService(ICriterionService criterionService) : ICriterionAppService
{
    public async Task<List<CriterionViewModel>> GetCriteriaAsync()
    {
        var criteria = await criterionService.GetAllAsync();

        return criteria.OrderBy(c => c.Title).Select(ToViewModel).ToList();
    }

    public async Task<CriterionViewModel> GetCriterionAsync(int criterionId) =>
        ToViewModel(await criterionService.GetRequiredAsync(criterionId));

    public async Task<bool> SaveCriterionAsync(CriterionViewModel criterion)
    {
        var title = InputValidation.RequiredText(criterion.Title, "Criterion title");

        var saved = await criterionService.SaveAsync(criterion.Id, c => c.Title = title);

        criterion.Id = saved.Id;
        criterion.Title = saved.Title;

        return true;
    }

    // Критерий, по которому уже есть оценки в отзывах, удалить нельзя (ограничение базы).
    public async Task DeleteCriterionAsync(int criterionId) =>
        await criterionService.DeleteRequiredAsync(criterionId);

    private static CriterionViewModel ToViewModel(Criterion criterion) =>
        new()
        {
            Id = criterion.Id,
            Title = criterion.Title
        };
}
