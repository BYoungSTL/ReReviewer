using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReReviewer.Application.Interfaces;
using ReReviewer.Application.ViewModels;
using ReReviewer.Controllers.Base;
using ReReviewer.Security;

namespace ReReviewer.Controllers;

public class AuthController(
    IAuthAppService authAppService,
    IUserAppService userAppService,
    IJwtTokenService tokenService) : BaseController
{
    // Создаёт аккаунт. Токен не выдаёт — после регистрации нужно войти через login.
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType<UserViewModel>(StatusCodes.Status201Created)]
    public async Task<ActionResult<UserViewModel>> Register(RegisterViewModel register)
    {
        var user = await authAppService.RegisterAsync(register);

        return CreatedAtAction(nameof(Me), user);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthTokenViewModel>> Login(LoginViewModel login)
    {
        var user = await authAppService.LoginAsync(login);

        return tokenService.CreateToken(user);
    }

    // Текущий пользователь по токену: Id, имя, почта, роли.
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserViewModel>> Me() =>
        await userAppService.GetUserAsync(CurrentUserId);
}