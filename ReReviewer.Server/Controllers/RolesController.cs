using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReReviewer.Application.Interfaces;
using ReReviewer.Application.ViewModels;
using ReReviewer.Controllers.Base;
using ReReviewer.Security;

namespace ReReviewer.Controllers;

// Только для администратора. Названия ролей Admin и Moderator зашиты в RoleNames:
// если их переименовать здесь, перестанут работать права в контроллерах.
[Authorize(Roles = RoleNames.Admin)]
public class RolesController(IRoleAppService roleAppService) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<List<RoleViewModel>>> GetAll() =>
        await roleAppService.GetRolesAsync();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoleViewModel>> Get(int id) =>
        await roleAppService.GetRoleAsync(id);

    [HttpPost]
    [ProducesResponseType<RoleViewModel>(StatusCodes.Status201Created)]
    public async Task<ActionResult<RoleViewModel>> Create(RoleViewModel role)
    {
        role.Id = null;
        await roleAppService.SaveRoleAsync(role);

        return CreatedAtAction(nameof(Get), new { id = role.Id }, await roleAppService.GetRoleAsync(role.Id!.Value));
    }

    // Id берётся из адреса, Id в теле запроса игнорируется.
    [HttpPut("{id:int}")]
    public async Task<ActionResult<RoleViewModel>> Update(int id, RoleViewModel role)
    {
        role.Id = id;
        await roleAppService.SaveRoleAsync(role);

        return await roleAppService.GetRoleAsync(id);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id)
    {
        await roleAppService.DeleteRoleAsync(id);

        return NoContent();
    }
}