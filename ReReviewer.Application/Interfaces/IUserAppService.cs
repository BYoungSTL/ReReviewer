using ReReviewer.Application.ViewModels;

namespace ReReviewer.Application.Interfaces;

public interface IUserAppService
{
    public Task<List<UserViewModel>> GetUsersAsync();
    public Task<UserViewModel> GetUserAsync(Guid userId);
    public Task<bool> SaveUserAsync(UserViewModel user);
    public Task DeleteUserAsync(Guid userId);

    public Task<UserViewModel> SetUserRolesAsync(Guid userId, List<int> roleIds);
}