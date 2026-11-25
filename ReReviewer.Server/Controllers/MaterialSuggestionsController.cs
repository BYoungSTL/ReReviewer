using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReReviewer.Application.Interfaces;
using ReReviewer.Application.ViewModels;
using ReReviewer.Controllers.Base;
using ReReviewer.Security;

namespace ReReviewer.Controllers;

// Заявки на новые материалы обзорщиков: предлагает любой вошедший (автор — всегда из токена), рассматривают модераторы.
// Материал появляется в api/reviewers/{id}/materials только после одобрения. Модераторы по-прежнему могут добавить
// материал сами, без заявки, — api/materials.
[Authorize]
public class MaterialSuggestionsController(IMaterialSuggestionAppService suggestionAppService) : BaseController
{
    [HttpPost]
    [ProducesResponseType<MaterialSuggestionViewModel>(StatusCodes.Status201Created)]
    public async Task<ActionResult<MaterialSuggestionViewModel>> Create(MaterialSuggestionViewModel suggestion) =>
        StatusCode(StatusCodes.Status201Created, await suggestionAppService.SuggestMaterialAsync(CurrentUserId, suggestion));

    // Свои заявки в любом статусе.
    [HttpGet("my")]
    public async Task<ActionResult<List<MaterialSuggestionViewModel>>> GetMy() =>
        await suggestionAppService.GetUserSuggestionsAsync(CurrentUserId);

    // Очередь: первой — заявка, которая ждёт дольше всех.
    [Authorize(Roles = RoleNames.Staff)]
    [HttpGet("moderation")]
    public async Task<ActionResult<PagedViewModel<MaterialSuggestionViewModel>>> GetModerationQueue(int page = 1, int pageSize = 20) =>
        await suggestionAppService.GetModerationQueueAsync(page, pageSize);

    // Создаёт материал обзорщика из заявки.
    [Authorize(Roles = RoleNames.Staff)]
    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Approve(Guid id)
    {
        await suggestionAppService.ModerateAsync(id, approve: true);

        return NoContent();
    }

    [Authorize(Roles = RoleNames.Staff)]
    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Reject(Guid id)
    {
        await suggestionAppService.ModerateAsync(id, approve: false);

        return NoContent();
    }
}
