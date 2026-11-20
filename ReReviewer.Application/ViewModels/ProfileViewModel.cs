namespace ReReviewer.Application.ViewModels;

// Свой профиль: имя и почта. Почта — это логин, поэтому её смену подтверждает текущий пароль.
public class ProfileViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // Нужен, только если меняется почта.
    public string? CurrentPassword { get; set; }
}

public class PasswordChangeViewModel
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

// Картинка аватарки для ответа API (api/users/{id}/avatar), не для JSON.
public class AvatarViewModel
{
    public byte[] Content { get; set; } = [];
    public string ContentType { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}
