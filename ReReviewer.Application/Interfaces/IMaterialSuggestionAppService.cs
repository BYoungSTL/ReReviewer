using ReReviewer.Application.ViewModels;

namespace ReReviewer.Application.Interfaces;

public interface IMaterialSuggestionAppService
{
    public Task<MaterialSuggestionViewModel> SuggestMaterialAsync(Guid userId, MaterialSuggestionViewModel suggestion);
    public Task<List<MaterialSuggestionViewModel>> GetUserSuggestionsAsync(Guid userId);

    public Task<PagedViewModel<MaterialSuggestionViewModel>> GetModerationQueueAsync(int page = 1, int pageSize = 20);

    // approve — создать материал из заявки, иначе отклонить.
    public Task ModerateAsync(Guid suggestionId, bool approve);
}
