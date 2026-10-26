using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReReviewer.Application.Interfaces;
using ReReviewer.Application.ViewModels;
using ReReviewer.Controllers.Base;
using ReReviewer.Security;

namespace ReReviewer.Controllers;

// Управление пользователями — только администратор. Свой профиль любой вошедший пользователь
// меняет через PUT api/users/me, смотрит — через GET api/auth/me. Регистрация — в api/auth/register.
[Authorize]
public class UsersController(IUserAppService userAppService) : BaseController
{
    #region Own profile

    // Имя, почта и, если передан, новый пароль. Роли здесь не меняются.
    [HttpPut("me")]
    public async Task<ActionResult<UserViewModel>> UpdateMe(UserViewModel user)
    {
        user.Id = CurrentUserId;
        await userAppService.SaveUserAsync(user);

        return await userAppService.GetUserAsync(CurrentUserId);
    }

    #endregion

    #region Administration

    [Authorize(Roles = RoleNames.Admin)]
    [HttpGet]
    public async Task<ActionResult<List<UserViewModel>>> GetAll() =>
        await userAppService.GetUsersAsync();

    [Authorize(Roles = RoleNames.Admin)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserViewModel>> Get(Guid id) =>
        await userAppService.GetUserAsync(id);

    [Authorize(Roles = RoleNames.Admin)]
    [HttpPost]
    [ProducesResponseType<UserViewModel>(StatusCodes.Status201Created)]
    public async Task<ActionResult<UserViewModel>> Create(UserViewModel user)
    {
        user.Id = null;
        await userAppService.SaveUserAsync(user);

        return CreatedAtAction(nameof(Get), new { id = user.Id }, await userAppService.GetUserAsync(user.Id!.Value));
    }

    // Id берётся из адреса, Id в теле запроса игнорируется.
    [Authorize(Roles = RoleNames.Admin)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserViewModel>> Update(Guid id, UserViewModel user)
    {
        user.Id = id;
        await userAppService.SaveUserAsync(user);

        return await userAppService.GetUserAsync(id);
    }

    [Authorize(Roles = RoleNames.Admin)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await userAppService.DeleteUserAsync(id);

        return NoContent();
    }

    // Тело запроса — массив Id ролей: у пользователя останутся ровно они.
    [Authorize(Roles = RoleNames.Admin)]
    [HttpPut("{id:guid}/roles")]
    public async Task<ActionResult<UserViewModel>> SetRoles(Guid id, List<int> roleIds) =>
        await userAppService.SetUserRolesAsync(id, roleIds);

    #endregion
}