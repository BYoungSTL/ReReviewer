namespace ReReviewer.Application.ViewModels;

public class AuthTokenViewModel
{
    // JWT для заголовка Authorization: Bearer <Token>.
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }

    public UserViewModel User { get; set; } = new();
}