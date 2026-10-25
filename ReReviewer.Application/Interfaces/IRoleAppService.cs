using ReReviewer.Application.ViewModels;

namespace ReReviewer.Application.Interfaces;

public interface IRoleAppService
{
    public Task<List<RoleViewModel>> GetRolesAsync();
    public Task<RoleViewModel> GetRoleAsync(int roleId);
    public Task<bool> SaveRoleAsync(RoleViewModel role);
    public Task DeleteRoleAsync(int roleId);
}