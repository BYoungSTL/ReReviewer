namespace ReReviewer.Application.ViewModels;

// Отдельная модель вместо UserViewModel: в ней нет Id, поэтому регистрацией нельзя изменить чужой аккаунт.
public class RegisterViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}