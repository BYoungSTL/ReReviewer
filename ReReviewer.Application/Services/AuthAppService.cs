using System.Security.Authentication;
using ReReviewer.Application.Interfaces;
using ReReviewer.Application.Security;
using ReReviewer.Application.ViewModels;
using ReReviewer.Data.Interfaces;

namespace ReReviewer.Application.Services;

// Проверка учётных данных и регистрация. Сам токен выпускает слой Server: Application не знает о JWT.
public class AuthAppService(IUserService userService, IUserAppService userAppService) : IAuthAppService
{
    // Для несуществующей почты пароль всё равно проверяется — против этого хэша, чтобы по времени ответа
    // нельзя было понять, зарегистрирована ли почта.
    private static readonly Lazy<string> DummyPasswordHash = new(() => PasswordHasher.Hash(Guid.NewGuid().ToString()));

    // Проверки имени, почты и пароля, хэширование — в UserAppService.SaveUserAsync.
    public async Task<UserViewModel> RegisterAsync(RegisterViewModel register)
    {
        var user = new UserViewModel
        {
            Name = register.Name,
            Email = register.Email,
            Password = register.Password
        };

        await userAppService.SaveUserAsync(user);

        return await userAppService.GetUserAsync(user.Id!.Value);
    }

    // Неверная почта и неверный пароль дают одну и ту же ошибку — AuthenticationException (401).
    public async Task<UserViewModel> LoginAsync(LoginViewModel login)
    {
        var user = string.IsNullOrWhiteSpace(login.Email)
            ? null
            : await userService.GetByEmailAsync(login.Email.Trim());

        var passwordMatches = PasswordHasher.Verify(login.Password ?? string.Empty, user?.PasswordHash ?? DummyPasswordHash.Value);
        if (user is null || !passwordMatches)
            throw new AuthenticationException("Invalid email or password.");

        return await userAppService.GetUserAsync(user.Id);
    }
}