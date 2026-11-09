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

            if (passwordHash is null)
                return;

            u.PasswordHash = passwordHash;
            // Смена пароля у существующего пользователя завершает его сеансы: токены, выданные раньше, не примутся.
            if (user.Id is not null)
                u.PasswordChangedAt = DateTime.UtcNow;
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

    // Заблокированный не может войти, а выданные ему токены перестают приниматься (AcceptsTokenAsync, см. Startup).
    // Себя заблокировать нельзя, иначе администратор сам потеряет доступ. Повторная блокировка не сдвигает дату.
    public async Task<UserViewModel> BlockUserAsync(Guid userId, Guid currentUserId)
    {
        if (userId == currentUserId)
            throw new ValidationException("You cannot block yourself.");

        var user = await userService.GetRequiredAsync(userId);

        user.BlockedAt ??= DateTime.UtcNow;
        await userService.UpdateAsync(user);

        return ToViewModel(user);
    }

    public async Task<UserViewModel> UnblockUserAsync(Guid userId)
    {
        var user = await userService.GetRequiredAsync(userId);

        user.BlockedAt = null;
        await userService.UpdateAsync(user);

        return ToViewModel(user);
    }

    public async Task<bool> AcceptsTokenAsync(Guid userId, DateTime issuedAt) =>
        await userService.AcceptsTokenAsync(userId, issuedAt);

    #region Own profile

    // Почта — это логин, поэтому её смену подтверждает текущий пароль: с чужого открытого сеанса
    // нельзя увести аккаунт, поменяв почту. Имя меняется без пароля.
    public async Task<UserViewModel> UpdateProfileAsync(Guid userId, ProfileViewModel profile)
    {
        var user = await userService.GetRequiredAsync(userId);

        if (!string.Equals(profile.Email?.Trim(), user.Email, StringComparison.OrdinalIgnoreCase))
            EnsureCurrentPassword(user, profile.CurrentPassword, "Current password is required to change email.");

        await SaveUserAsync(new UserViewModel { Id = userId, Name = profile.Name, Email = profile.Email });

        return await GetUserAsync(userId);
    }

    public async Task<UserViewModel> ChangePasswordAsync(Guid userId, PasswordChangeViewModel change)
    {
        var user = await userService.GetRequiredAsync(userId);

        EnsureCurrentPassword(user, change.CurrentPassword, "Current password is required.");
        if (string.IsNullOrEmpty(change.NewPassword))
            throw new ValidationException("New password is required.");

        await SaveUserAsync(new UserViewModel { Id = userId, Name = user.Name, Email = user.Email, Password = change.NewPassword });

        return await GetUserAsync(userId);
    }

    public async Task<UserViewModel> SetAvatarAsync(Guid userId, byte[] content)
    {
        if (content.Length == 0)
            throw new ValidationException("Avatar file is empty.");
        if (content.Length > AvatarImage.MaxBytes)
            throw new ValidationException($"Avatar must be at most {AvatarImage.MaxBytes / 1024} KB.");

        var contentType = AvatarImage.DetectContentType(content)
            ?? throw new ValidationException("Avatar must be a PNG, JPEG or WebP image.");

        return ToViewModel(await userService.SetAvatarAsync(userId, content, contentType));
    }

    public async Task<UserViewModel> RemoveAvatarAsync(Guid userId) =>
        ToViewModel(await userService.RemoveAvatarAsync(userId));

    public async Task<AvatarViewModel> GetAvatarAsync(Guid userId)
    {
        var avatar = await userService.GetAvatarAsync(userId)
            ?? throw new KeyNotFoundException("Avatar not found.");

        return new AvatarViewModel { Content = avatar.Content, ContentType = avatar.ContentType, UpdatedAt = avatar.UpdatedAt };
    }

    private static void EnsureCurrentPassword(User user, string? currentPassword, string missingMessage)
    {
        if (string.IsNullOrEmpty(currentPassword))
            throw new ValidationException(missingMessage);
        if (!PasswordHasher.Verify(currentPassword, user.PasswordHash))
            throw new ValidationException("Current password is incorrect.");
    }

    #endregion

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
            CreatedAt = user.CreatedAt,
            BlockedAt = user.BlockedAt,
            AvatarUpdatedAt = user.AvatarUpdatedAt
        };
}