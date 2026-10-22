using ReReviewer.Application.Interfaces;
using ReReviewer.Application.Services.Base;
using ReReviewer.Application.ViewModels;
using ReReviewer.Data.Entities;
using ReReviewer.Data.Interfaces;

namespace ReReviewer.Application.Services;

public class CriterionAppService(ICriterionService criterionService, IGenreService genreService) : ICriterionAppService
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

        if (criterion.GenreId is { } genreId)
            await genreService.GetRequiredAsync(genreId);

        var saved = await criterionService.SaveAsync(criterion.Id, c =>
        {
            c.Title = title;
            c.GenreId = criterion.GenreId;
        });

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
            Title = criterion.Title,
            GenreId = criterion.GenreId,
            GenreTitle = criterion.Genre?.Title
        };
}