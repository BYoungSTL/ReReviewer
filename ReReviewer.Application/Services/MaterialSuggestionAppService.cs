using ReReviewer.Application.Interfaces;
using ReReviewer.Application.Services.Base;
using ReReviewer.Application.ViewModels;
using ReReviewer.Data.Entities;
using ReReviewer.Data.Interfaces;

namespace ReReviewer.Application.Services;

// Здесь — проверка ввода и сборка ViewModel. Существуют ли обзорщик и игра, свободна ли ссылка — проверяет
// IMaterialSuggestionService: ему нужна база. Обзорщик, игра и автор заявки подгружаются лениво.
public class MaterialSuggestionAppService(IMaterialSuggestionService suggestionService) : IMaterialSuggestionAppService
{
    private const int MaxPageSize = 100;

    public async Task<MaterialSuggestionViewModel> SuggestMaterialAsync(Guid userId, MaterialSuggestionViewModel suggestion)
    {
        var title = InputValidation.MaterialTitle(suggestion.Title);
        var url = InputValidation.MaterialUrl(suggestion.Url);
        var format = InputValidation.ParseMaterialFormat(suggestion.Format);

        var saved = await suggestionService.CreateAsync(
            userId, suggestion.ReviewerId, suggestion.GameId, title, url, format, suggestion.PublishedAt);

        return ToViewModel(saved);
    }

    public async Task<List<MaterialSuggestionViewModel>> GetUserSuggestionsAsync(Guid userId)
    {
        var suggestions = await suggestionService.GetByUserAsync(userId);

        return suggestions.Select(ToViewModel).ToList();
    }

    // Первой идёт заявка, которая ждёт дольше всех.
    public async Task<PagedViewModel<MaterialSuggestionViewModel>> GetModerationQueueAsync(int page = 1, int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var suggestions = await suggestionService.GetPendingAsync((page - 1) * pageSize, pageSize);
        var totalCount = await suggestionService.CountPendingAsync();

        return new()
        {
            Items = suggestions.Select(ToViewModel).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task ModerateAsync(Guid suggestionId, bool approve)
    {
        if (approve)
            await suggestionService.ApproveAsync(suggestionId);
        else
            await suggestionService.RejectAsync(suggestionId);
    }

    private static MaterialSuggestionViewModel ToViewModel(MaterialSuggestion suggestion) =>
        new()
        {
            Id = suggestion.Id,
            ReviewerId = suggestion.ReviewerId,
            GameId = suggestion.GameId,
            Title = suggestion.Title,
            Url = suggestion.Url,
            Format = suggestion.Format.ToString(),
            PublishedAt = suggestion.PublishedAt,
            ReviewerName = suggestion.Reviewer.FullName,
            GameTitle = suggestion.Game.Title,
            UserId = suggestion.UserId,
            UserName = suggestion.User.Name,
            Status = suggestion.Status.ToString(),
            CreatedAt = suggestion.CreatedAt,
            DecidedAt = suggestion.DecidedAt,
            MaterialId = suggestion.MaterialId
        };
}
