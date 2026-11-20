using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReReviewer.Application.Interfaces;
using ReReviewer.Application.ViewModels;
using ReReviewer.Controllers.Base;
using ReReviewer.Security;

namespace ReReviewer.Controllers;

// Запросы привязать профиль обзорщика к своему аккаунту («это я»): отправляет вошедший, рассматривают модераторы.
// Модераторы по-прежнему могут привязать профиль сами, без запроса, — api/reviewers/{id}/validate/{userId}.
[Authorize]
public class ClaimsController(IReviewerRequestAppService requestAppService) : BaseController
{
    [HttpPost]
    [ProducesResponseType<ReviewerClaimViewModel>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ReviewerClaimViewModel>> Create(ReviewerClaimViewModel claim) =>
        StatusCode(StatusCodes.Status201Created, await requestAppService.ClaimReviewerAsync(CurrentUserId, claim));

    // Свои запросы в любом статусе.
    [HttpGet("my")]
    public async Task<ActionResult<List<ReviewerClaimViewModel>>> GetMy() =>
        await requestAppService.GetUserClaimsAsync(CurrentUserId);

    // Очередь: первым — запрос, который ждёт дольше всех.
    [Authorize(Roles = RoleNames.Staff)]
    [HttpGet("moderation")]
    public async Task<ActionResult<PagedViewModel<ReviewerClaimViewModel>>> GetModerationQueue(int page = 1, int pageSize = 20) =>
        await requestAppService.GetClaimQueueAsync(page, pageSize);

    // Привязывает профиль к автору запроса, остальные запросы на этот профиль отклоняются.
    [Authorize(Roles = RoleNames.Staff)]
    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Approve(Guid id)
    {
        await requestAppService.ModerateClaimAsync(id, approve: true);

        return NoContent();
    }

    [Authorize(Roles = RoleNames.Staff)]
    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Reject(Guid id)
    {
        await requestAppService.ModerateClaimAsync(id, approve: false);

        return NoContent();
    }
}
