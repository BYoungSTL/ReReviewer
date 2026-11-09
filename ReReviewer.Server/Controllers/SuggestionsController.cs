using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReReviewer.Application.Interfaces;
using ReReviewer.Application.ViewModels;
using ReReviewer.Controllers.Base;
using ReReviewer.Security;

namespace ReReviewer.Controllers;

// Заявки на новых обзорщиков: предлагает любой вошедший, рассматривают модераторы.
// Профиль появляется в api/reviewers только после одобрения, автор заявки — всегда из токена.
[Authorize]
public class SuggestionsController(IReviewerRequestAppService requestAppService) : BaseController
{
    [HttpPost]
    [ProducesResponseType<ReviewerSuggestionViewModel>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ReviewerSuggestionViewModel>> Create(ReviewerSuggestionViewModel suggestion) =>
        StatusCode(StatusCodes.Status201Created, await requestAppService.SuggestReviewerAsync(CurrentUserId, suggestion));

    // Свои заявки в любом статусе.
    [HttpGet("my")]
    public async Task<ActionResult<List<ReviewerSuggestionViewModel>>> GetMy() =>
        await requestAppService.GetUserSuggestionsAsync(CurrentUserId);

    // Очередь: первой — заявка, которая ждёт дольше всех.
    [Authorize(Roles = RoleNames.Staff)]
    [HttpGet("moderation")]
    public async Task<ActionResult<PagedViewModel<ReviewerSuggestionViewModel>>> GetModerationQueue(int page = 1, int pageSize = 20) =>
        await requestAppService.GetSuggestionQueueAsync(page, pageSize);

    // Создаёт профиль обзорщика; заявку «это я» привязывает к аккаунту автора.
    [Authorize(Roles = RoleNames.Staff)]
    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Approve(Guid id)
    {
        await requestAppService.ModerateSuggestionAsync(id, approve: true);

        return NoContent();
    }

    [Authorize(Roles = RoleNames.Staff)]
    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Reject(Guid id)
    {
        await requestAppService.ModerateSuggestionAsync(id, approve: false);

        return NoContent();
    }
}
