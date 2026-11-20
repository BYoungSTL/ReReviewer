using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReReviewer.Application.Interfaces;
using ReReviewer.Application.ViewModels;
using ReReviewer.Controllers.Base;
using ReReviewer.Security;

namespace ReReviewer.Controllers;

// Номинации обзорщиков на знаки: номинирует вошедший (автор — всегда из токена), решают модераторы.
// Модератор видит пары «обзорщик + знак» с числом номинаций и выдаёт знак или отклоняет номинации.
[Authorize]
public class NominationsController(IBadgeNominationAppService nominationAppService) : BaseController
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Nominate(BadgeNominationViewModel nomination)
    {
        await nominationAppService.NominateAsync(CurrentUserId, nomination);

        return NoContent();
    }

    // Отозвать свою номинацию.
    [HttpDelete("{reviewerId:guid}/{badgeId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Withdraw(Guid reviewerId, int badgeId)
    {
        await nominationAppService.WithdrawAsync(CurrentUserId, reviewerId, badgeId);

        return NoContent();
    }

    // Id знаков, на которые текущий пользователь номинировал обзорщика.
    [HttpGet("my")]
    public async Task<ActionResult<List<int>>> GetMy(Guid reviewerId) =>
        await nominationAppService.GetUserNominationsAsync(CurrentUserId, reviewerId);

    // Очередь: первой — пара с наибольшим числом номинаций.
    [Authorize(Roles = RoleNames.Staff)]
    [HttpGet("moderation")]
    public async Task<ActionResult<PagedViewModel<BadgeNominationSummaryViewModel>>> GetModerationQueue(int page = 1, int pageSize = 20) =>
        await nominationAppService.GetModerationQueueAsync(page, pageSize);

    // Выдаёт обзорщику знак, номинации пары закрываются.
    [Authorize(Roles = RoleNames.Staff)]
    [HttpPost("{reviewerId:guid}/{badgeId:int}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Approve(Guid reviewerId, int badgeId)
    {
        await nominationAppService.ModerateAsync(reviewerId, badgeId, approve: true);

        return NoContent();
    }

    [Authorize(Roles = RoleNames.Staff)]
    [HttpPost("{reviewerId:guid}/{badgeId:int}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Reject(Guid reviewerId, int badgeId)
    {
        await nominationAppService.ModerateAsync(reviewerId, badgeId, approve: false);

        return NoContent();
    }
}
