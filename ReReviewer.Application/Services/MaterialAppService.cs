using System.ComponentModel.DataAnnotations;
using ReReviewer.Application.Interfaces;
using ReReviewer.Application.Services.Base;
using ReReviewer.Application.ViewModels;
using ReReviewer.Data.Entities;
using ReReviewer.Data.Entities.Enums;
using ReReviewer.Data.Interfaces;

namespace ReReviewer.Application.Services;

// Обзорщик, игра и издание материала подгружаются лениво при сборке ViewModel.
public class MaterialAppService(
    IMaterialService materialService,
    IReviewerService reviewerService,
    IGameService gameService,
    IOutletService outletService) : IMaterialAppService
{
    #region Materials

    public async Task<MaterialViewModel> GetMaterialAsync(Guid materialId) =>
        ToViewModel(await materialService.GetRequiredAsync(materialId));

    public async Task<List<MaterialViewModel>> GetReviewerMaterialsAsync(Guid reviewerId)
    {
        var reviewer = await reviewerService.GetRequiredAsync(reviewerId);

        return ToViewModels(reviewer.Materials);
    }

    public async Task<List<MaterialViewModel>> GetGameMaterialsAsync(Guid gameId)
    {
        var game = await gameService.GetRequiredAsync(gameId);

        return ToViewModels(game.Materials);
    }

    // Без Id создаёт материал, с Id — правит существующий; обзорщик у существующего материала не меняется.
    public async Task<bool> SaveMaterialAsync(MaterialViewModel material)
    {
        var values = ToNewEntity(material);

        if (material.Id is null)
            await reviewerService.GetRequiredAsync(material.ReviewerId);

        await gameService.GetRequiredAsync(material.GameId);

        if (material.OutletId is { } outletId)
            await outletService.GetRequiredAsync(outletId);

        var saved = await materialService.SaveAsync(material.Id, m =>
        {
            if (material.Id is null)
                m.ReviewerId = material.ReviewerId;

            m.GameId = values.GameId;
            m.OutletId = values.OutletId;
            m.Title = values.Title;
            m.Url = values.Url;
            m.Format = values.Format;
            m.OriginalScore = values.OriginalScore;
            m.PublishedAt = values.PublishedAt;
        });

        material.Id = saved.Id;
        material.ReviewerId = saved.ReviewerId;
        material.CreatedAt = saved.CreatedAt;

        return true;
    }

    // Вместе с материалом пропадают и ссылки на него из отзывов; сами отзывы остаются.
    public async Task DeleteMaterialAsync(Guid materialId) =>
        await materialService.DeleteRequiredAsync(materialId);

    // Материалы без Id создаются у обзорщика, с Id — уже его материалы, они пропускаются.
    // Материал другого обзорщика — ValidationException. Возвращает все материалы обзорщика.
    public async Task<List<MaterialViewModel>> LinkMaterialsAsync(Guid reviewerId, List<MaterialViewModel> materials)
    {
        var entities = materials
            .Select(m => m.Id is { } id ? new Material { Id = id } : ToNewEntity(m))
            .ToList();

        var reviewer = await reviewerService.LinkMaterialsAsync(new Reviewer { Id = reviewerId }, entities);

        return ToViewModels(reviewer.Materials);
    }

    #endregion

    #region Helpers

    // Проверяет поля модели и переносит их в новую сущность (кроме обзорщика).
    private static Material ToNewEntity(MaterialViewModel material)
    {
        if (material.OriginalScore is < 0 or > 10)
            throw new ValidationException("Original score must be between 0 and 10.");

        return new Material
        {
            GameId = material.GameId,
            OutletId = material.OutletId,
            Title = InputValidation.RequiredText(material.Title, "Material title"),
            Url = InputValidation.RequiredText(material.Url, "Material url"),
            Format = ParseFormat(material.Format),
            OriginalScore = material.OriginalScore,
            PublishedAt = material.PublishedAt
        };
    }

    private static MaterialFormat ParseFormat(string format) =>
        Enum.TryParse<MaterialFormat>(format, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed)
            ? parsed
            : throw new ValidationException(
                $"Unknown material format '{format}'. Allowed: {string.Join(", ", Enum.GetNames<MaterialFormat>())}.");

    // Новые сверху: по дате публикации, затем по дате добавления на сайт.
    private static List<MaterialViewModel> ToViewModels(IEnumerable<Material> materials) =>
        materials
            .OrderByDescending(m => m.PublishedAt)
            .ThenByDescending(m => m.CreatedAt)
            .Select(ToViewModel)
            .ToList();

    private static MaterialViewModel ToViewModel(Material material) =>
        new()
        {
            Id = material.Id,
            ReviewerId = material.ReviewerId,
            GameId = material.GameId,
            OutletId = material.OutletId,
            Title = material.Title,
            Url = material.Url,
            Format = material.Format.ToString(),
            OriginalScore = material.OriginalScore,
            PublishedAt = material.PublishedAt,
            ReviewerName = material.Reviewer.FullName,
            GameTitle = material.Game.Title,
            OutletName = material.Outlet?.Name,
            CreatedAt = material.CreatedAt
        };

    #endregion
}