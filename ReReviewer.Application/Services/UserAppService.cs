using System.ComponentModel.DataAnnotations;
using ReReviewer.Application.Interfaces;
using ReReviewer.Application.Security;
using ReReviewer.Application.Services.Base;
using ReReviewer.Application.ViewModels;
using ReReviewer.Data.Entities;
using ReReviewer.Data.Interfaces;

namespace ReReviewer.Application.Services;

// Роли и профиль обзорщика подгружаются лениво при сборке ViewModel. Пароль хранится только хэшем.
public class UserAppService(IUserService userService, IRoleService roleService) : IUserAppService
{
    private const int MinPasswordLength = 8;

    public async Task<List<UserViewModel>> GetUsersAsync()
    {
        var users = await userService.GetAllAsync();

        return users.OrderBy(u => u.Name).Select(ToViewModel).ToList();
    }

    public async Task<UserViewModel> GetUserAsync(Guid userId) =>
        ToViewModel(await userService.GetRequiredAsync(userId));

    // Без Id создаёт пользователя (пароль обязателен), с Id — правит имя, почту и, если передан, пароль.
    // Роли здесь не меняются — для этого SetUserRolesAsync.
    public async Task<bool> SaveUserAsync(UserViewModel user)
    {
        var name = InputValidation.RequiredText(user.Name, "User name");
        var email = InputValidation.RequiredText(user.Email, "Email");
        if (!new EmailAddressAttribute().IsValid(email))
            throw new ValidationException("Email is not valid.");

        var password = string.IsNullOrEmpty(user.Password) ? null : user.Password;
        if (user.Id is null && password is null)
            throw new ValidationException("Password is required.");
        if (password is { Length: < MinPasswordLength })
            throw new ValidationException($"Password must be at least {MinPasswordLength} characters long.");

        var sameEmail = await userService.GetByEmailAsync(email);
        if (sameEmail is not null && sameEmail.Id != user.Id)
            throw new ValidationException("Email is already in use.");

        var passwordHash = password is null ? null : PasswordHasher.Hash(password);

        var saved = await userService.SaveAsync(user.Id, u =>
        {
            u.Name = name;
            u.Email = email;

            if (passwordHash is not null)
                u.PasswordHash = passwordHash;
        });

        user.Id = saved.Id;
        user.Name = saved.Name;
        user.Email = saved.Email;
        user.Password = null;
        user.CreatedAt = saved.CreatedAt;

        return true;
    }

    // Вместе с пользователем удаляются его отзывы; профиль обзорщика остаётся без привязки.
    public async Task DeleteUserAsync(Guid userId) =>
        await userService.DeleteRequiredAsync(userId);

    public async Task<UserViewModel> SetUserRolesAsync(Guid userId, List<int> roleIds)
    {
        var user = await userService.GetRequiredAsync(userId);
        var roles = await roleService.GetByIdsAsync(roleIds);

        user.Roles.SyncWith(roles);
        await userService.UpdateAsync(user);

        return ToViewModel(user);
    }

    private static UserViewModel ToViewModel(User user) =>
        new()
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Roles = user.Roles
                .OrderBy(r => r.Title)
                .Select(r => new RoleViewModel { Id = r.Id, Title = r.Title })
                .ToList(),
            ReviewerId = user.Reviewer?.Id,
            CreatedAt = user.CreatedAt
        };
}