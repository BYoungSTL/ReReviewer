using ReReviewer.Application.ViewModels;

namespace ReReviewer.Security;

public interface IJwtTokenService
{
    public AuthTokenViewModel CreateToken(UserViewModel user);
}