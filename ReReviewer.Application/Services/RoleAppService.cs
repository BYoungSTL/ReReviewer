using ReReviewer.Application.Interfaces;
using ReReviewer.Application.Services.Base;
using ReReviewer.Application.ViewModels;
using ReReviewer.Data.Entities;
using ReReviewer.Data.Interfaces;

namespace ReReviewer.Application.Services;

public class RoleAppService(IRoleService roleService) : IRoleAppService
{
    public async Task<List<RoleViewModel>> GetRolesAsync()
    {
        var roles = await roleService.GetAllAsync();

        return roles.OrderBy(r => r.Title).Select(ToViewModel).ToList();
    }

    public async Task<RoleViewModel> GetRoleAsync(int roleId) =>
        ToViewModel(await roleService.GetRequiredAsync(roleId));

    public async Task<bool> SaveRoleAsync(RoleViewModel role)
    {
        var title = InputValidation.RequiredText(role.Title, "Role title");

        var saved = await roleService.SaveAsync(role.Id, r => r.Title = title);

        role.Id = saved.Id;
        role.Title = saved.Title;

        return true;
    }

    // Пользователи с этой ролью остаются, у них просто пропадает роль.
    public async Task DeleteRoleAsync(int roleId) =>
        await roleService.DeleteRequiredAsync(roleId);

    private static RoleViewModel ToViewModel(Role role) =>
        new()
        {
            Id = role.Id,
            Title = role.Title
        };
}