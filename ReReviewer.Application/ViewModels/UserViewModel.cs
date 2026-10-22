namespace ReReviewer.Application.ViewModels;

public class UserViewModel
{
    // Null — новый пользователь, иначе — правка существующего.
    public Guid? Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // Только запись: обязателен при создании, при правке — если пароль меняется. В ответах всегда null.
    public string? Password { get; set; }

    #region Только чтение

    // Роли меняются отдельно, через IUserAppService.SetUserRolesAsync.
    public List<RoleViewModel> Roles { get; set; } = [];

    // Профиль обзорщика, к которому привязан аккаунт.
    public Guid? ReviewerId { get; set; }
    public DateTime CreatedAt { get; set; }

    #endregion
}