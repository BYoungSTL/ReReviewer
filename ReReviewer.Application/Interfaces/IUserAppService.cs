using ReReviewer.Application.ViewModels;

namespace ReReviewer.Application.Interfaces;

public interface IUserAppService
{
    public Task<List<UserViewModel>> GetUsersAsync();
    public Task<UserViewModel> GetUserAsync(Guid userId);
    public Task<bool> SaveUserAsync(UserViewModel user);
    public Task DeleteUserAsync(Guid userId);

    public Task<UserViewModel> SetUserRolesAsync(Guid userId, List<int> roleIds);

    public Task<UserViewModel> BlockUserAsync(Guid userId, Guid currentUserId);
    public Task<UserViewModel> UnblockUserAsync(Guid userId);
    public Task<bool> AcceptsTokenAsync(Guid userId, DateTime issuedAt);

    public Task<UserViewModel> UpdateProfileAsync(Guid userId, ProfileViewModel profile);
    public Task<UserViewModel> ChangePasswordAsync(Guid userId, PasswordChangeViewModel change);

    public Task<UserViewModel> SetAvatarAsync(Guid userId, byte[] content);
    public Task<UserViewModel> RemoveAvatarAsync(Guid userId);
    public Task<AvatarViewModel> GetAvatarAsync(Guid userId);
}