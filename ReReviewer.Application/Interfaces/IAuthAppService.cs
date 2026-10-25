using ReReviewer.Application.ViewModels;

namespace ReReviewer.Application.Interfaces;

public interface IAuthAppService
{
    public Task<UserViewModel> RegisterAsync(RegisterViewModel register);
    public Task<UserViewModel> LoginAsync(LoginViewModel login);
}