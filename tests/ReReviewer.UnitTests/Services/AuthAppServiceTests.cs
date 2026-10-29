using System.Security.Authentication;
using ReReviewer.Application.Interfaces;
using ReReviewer.Application.Security;
using ReReviewer.Application.Services;
using ReReviewer.Application.ViewModels;
using ReReviewer.Data.Entities;
using ReReviewer.Data.Interfaces;

namespace ReReviewer.UnitTests.Services;

public class AuthAppServiceTests
{
    private const string Password = "secret-123";
    private const string Email = "player@test.local";

    private readonly IUserService _users = Substitute.For<IUserService>();
    private readonly IUserAppService _userApp = Substitute.For<IUserAppService>();
    private readonly AuthAppService _auth;
    private readonly User _user = new() { Id = Guid.NewGuid(), Name = "player", Email = Email, PasswordHash = PasswordHasher.Hash(Password) };

    public AuthAppServiceTests()
    {
        _auth = new AuthAppService(_users, _userApp);
        _users.GetByEmailAsync(Email, Arg.Any<CancellationToken>()).Returns(_user);
        _userApp.GetUserAsync(_user.Id).Returns(new UserViewModel { Id = _user.Id, Email = Email });
    }

    #region LoginAsync

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsUser()
    {
        var result = await _auth.LoginAsync(new LoginViewModel { Email = Email, Password = Password });

        result.Id.ShouldBe(_user.Id);
    }

    [Fact]
    public async Task LoginAsync_TrimsEmail()
    {
        await _auth.LoginAsync(new LoginViewModel { Email = $"  {Email}  ", Password = Password });

        await _users.Received(1).GetByEmailAsync(Email, Arg.Any<CancellationToken>());
    }

    // Неверный пароль и неизвестная почта неразличимы для клиента.
    [Theory]
    [InlineData(Email, "wrong-password")]
    [InlineData("nobody@test.local", Password)]
    public async Task LoginAsync_InvalidCredentials_ThrowsSameError(string email, string password)
    {
        var exception = await Should.ThrowAsync<AuthenticationException>(
            () => _auth.LoginAsync(new LoginViewModel { Email = email, Password = password }));

        exception.Message.ShouldBe("Invalid email or password.");
        await _userApp.DidNotReceiveWithAnyArgs().GetUserAsync(default);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task LoginAsync_EmptyEmail_ThrowsWithoutLookup(string email)
    {
        await Should.ThrowAsync<AuthenticationException>(() => _auth.LoginAsync(new LoginViewModel { Email = email, Password = Password }));

        await _users.DidNotReceiveWithAnyArgs().GetByEmailAsync(default!, default);
    }

    #endregion

    #region RegisterAsync

    [Fact]
    public async Task RegisterAsync_CreatesNewUser_AndReturnsSavedProfile()
    {
        var newId = Guid.NewGuid();
        _userApp.SaveUserAsync(Arg.Any<UserViewModel>()).Returns(ci =>
        {
            ci.Arg<UserViewModel>().Id = newId;
            return true;
        });
        _userApp.GetUserAsync(newId).Returns(new UserViewModel { Id = newId, Name = "new" });

        var result = await _auth.RegisterAsync(new RegisterViewModel { Name = "new", Email = "new@test.local", Password = Password });

        result.Id.ShouldBe(newId);
        await _userApp.Received(1).SaveUserAsync(Arg.Is<UserViewModel>(u =>
            u.Name == "new" && u.Email == "new@test.local" && u.Password == Password));
    }

    #endregion
}