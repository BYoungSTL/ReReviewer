using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReReviewer.Application.Interfaces;
using ReReviewer.Application.ViewModels;
using ReReviewer.Controllers.Base;
using ReReviewer.Security;

namespace ReReviewer.Controllers;

// Управление пользователями — только администратор. Свой профиль любой вошедший пользователь
// меняет через api/users/me, смотрит — через GET api/auth/me. Регистрация — в api/auth/register.
[Authorize]
public class UsersController(IUserAppService userAppService, IJwtTokenService tokenService) : BaseController
{
    #region Own profile

    // Изменения своего профиля возвращают новый токен: в нём имя, почта и версия аватарки, которые показывает сайт.

    // Имя и почта. Почту можно сменить только с текущим паролем.
    [HttpPut("me")]
    public async Task<ActionResult<AuthTokenViewModel>> UpdateMe(ProfileViewModel profile) =>
        tokenService.CreateToken(await userAppService.UpdateProfileAsync(CurrentUserId, profile));

    // Остальные сеансы после смены пароля завершаются: их токены выданы раньше (см. Startup, OnTokenValidated).
    [HttpPut("me/password")]
    public async Task<ActionResult<AuthTokenViewModel>> ChangePassword(PasswordChangeViewModel change) =>
        tokenService.CreateToken(await userAppService.ChangePasswordAsync(CurrentUserId, change));

    // PNG, JPEG или WebP до 512 КБ, тип определяется по содержимому файла.
    [HttpPut("me/avatar")]
    [RequestSizeLimit(1024 * 1024)]
    public async Task<ActionResult<AuthTokenViewModel>> SetAvatar(IFormFile file)
    {
        using var content = new MemoryStream();
        await file.CopyToAsync(content);

        return tokenService.CreateToken(await userAppService.SetAvatarAsync(CurrentUserId, content.ToArray()));
    }

    [HttpDelete("me/avatar")]
    public async Task<ActionResult<AuthTokenViewModel>> RemoveAvatar() =>
        tokenService.CreateToken(await userAppService.RemoveAvatarAsync(CurrentUserId));

    // Аватарка видна всем. Сайт добавляет к адресу версию (?v=), поэтому её можно кэшировать надолго.
    // nosniff и пустой CSP — чтобы картинку нельзя было открыть как страницу.
    [AllowAnonymous]
    [HttpGet("{id:guid}/avatar")]
    [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK, "image/png", "image/jpeg", "image/webp")]
    public async Task<IActionResult> GetAvatar(Guid id)
    {
        var avatar = await userAppService.GetAvatarAsync(id);

        Response.Headers.CacheControl = "public, max-age=86400";
        Response.Headers.XContentTypeOptions = "nosniff";
        Response.Headers.ContentSecurityPolicy = "default-src 'none'";

        return File(avatar.Content, avatar.ContentType);
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

    // Заблокированный не может войти, а выданные ему токены сразу перестают действовать. Себя заблокировать нельзя.
    // Отзывы пользователя остаются как есть: снять опубликованные можно через модерацию.
    [Authorize(Roles = RoleNames.Admin)]
    [HttpPost("{id:guid}/block")]
    public async Task<ActionResult<UserViewModel>> Block(Guid id) =>
        await userAppService.BlockUserAsync(id, CurrentUserId);

    [Authorize(Roles = RoleNames.Admin)]
    [HttpPost("{id:guid}/unblock")]
    public async Task<ActionResult<UserViewModel>> Unblock(Guid id) =>
        await userAppService.UnblockUserAsync(id);

    #endregion
}