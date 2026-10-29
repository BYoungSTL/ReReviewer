using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReReviewer.Application.Interfaces;
using ReReviewer.Application.ViewModels;
using ReReviewer.Controllers.Base;
using ReReviewer.Security;

namespace ReReviewer.Controllers;

// Читать может кто угодно, менять — администратор или модератор.
[Authorize(Roles = RoleNames.Staff)]
public class BadgesController(IBadgeAppService badgeAppService) : BaseController
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<List<BadgeViewModel>>> GetAll() =>
        await badgeAppService.GetBadgesAsync();

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<BadgeViewModel>> Get(int id) =>
        await badgeAppService.GetBadgeAsync(id);

    [HttpPost]
    [ProducesResponseType<BadgeViewModel>(StatusCodes.Status201Created)]
    public async Task<ActionResult<BadgeViewModel>> Create(BadgeViewModel badge)
    {
        badge.Id = null;
        await badgeAppService.SaveBadgeAsync(badge);

        return CreatedAtAction(nameof(Get), new { id = badge.Id }, await badgeAppService.GetBadgeAsync(badge.Id!.Value));
    }

    // Id берётся из адреса, Id в теле запроса игнорируется.
    [HttpPut("{id:int}")]
    public async Task<ActionResult<BadgeViewModel>> Update(int id, BadgeViewModel badge)
    {
        badge.Id = id;
        await badgeAppService.SaveBadgeAsync(badge);

        return await badgeAppService.GetBadgeAsync(id);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id)
    {
        await badgeAppService.DeleteBadgeAsync(id);

        return NoContent();
    }
}