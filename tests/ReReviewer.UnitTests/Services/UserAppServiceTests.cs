using System.ComponentModel.DataAnnotations;
using ReReviewer.Application.Security;
using ReReviewer.Application.Services;
using ReReviewer.Application.ViewModels;
using ReReviewer.Data.Entities;
using ReReviewer.Data.Interfaces;

namespace ReReviewer.UnitTests.Services;

public class UserAppServiceTests
{
    private readonly IUserService _users = Substitute.For<IUserService>();
    private readonly IRoleService _roles = Substitute.For<IRoleService>();
    private readonly UserAppService _service;

    public UserAppServiceTests()
    {
        _service = new UserAppService(_users, _roles);
        _users.AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<User>());
    }

    #region Создание

    [Fact]
    public async Task SaveUserAsync_New_StoresHashAndClearsPassword()
    {
        var user = new UserViewModel { Name = " player ", Email = " player@test.local ", Password = "secret-123" };

        (await _service.SaveUserAsync(user)).ShouldBeTrue();

        user.Password.ShouldBeNull();
        await _users.Received(1).AddAsync(Arg.Is<User>(u =>
            u.Name == "player"
            && u.Email == "player@test.local"
            && u.PasswordHash != "secret-123"
            && PasswordHasher.Verify("secret-123", u.PasswordHash)), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task SaveUserAsync_NewWithoutPassword_Throws(string? password)
    {
        var exception = await Should.ThrowAsync<ValidationException>(
            () => _service.SaveUserAsync(new UserViewModel { Name = "player", Email = "player@test.local", Password = password }));

        exception.Message.ShouldBe("Password is required.");
    }

    [Fact]
    public async Task SaveUserAsync_ShortPassword_Throws()
    {
        var exception = await Should.ThrowAsync<ValidationException>(
            () => _service.SaveUserAsync(new UserViewModel { Name = "player", Email = "player@test.local", Password = "1234567" }));

        exception.Message.ShouldBe("Password must be at least 8 characters long.");
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("player@")]
    public async Task SaveUserAsync_InvalidEmail_Throws(string email)
    {
        var exception = await Should.ThrowAsync<ValidationException>(
            () => _service.SaveUserAsync(new UserViewModel { Name = "player", Email = email, Password = "secret-123" }));

        exception.Message.ShouldBe("Email is not valid.");
    }

    [Fact]
    public async Task SaveUserAsync_EmailOfAnotherUser_Throws()
    {
        _users.GetByEmailAsync("taken@test.local", Arg.Any<CancellationToken>()).Returns(new User { Id = Guid.NewGuid() });

        var exception = await Should.ThrowAsync<ValidationException>(
            () => _service.SaveUserAsync(new UserViewModel { Name = "player", Email = "taken@test.local", Password = "secret-123" }));

        exception.Message.ShouldBe("Email is already in use.");
        await _users.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    #endregion

    #region Правка

    [Fact]
    public async Task SaveUserAsync_UpdateWithoutPassword_KeepsHash()
    {
        var existing = new User { Id = Guid.NewGuid(), Name = "old", Email = "player@test.local", PasswordHash = "old-hash" };
        _users.GetByIdAsync(existing.Id, Arg.Any<CancellationToken>()).Returns(existing);
        _users.GetByEmailAsync("player@test.local", Arg.Any<CancellationToken>()).Returns(existing);

        await _service.SaveUserAsync(new UserViewModel { Id = existing.Id, Name = "new", Email = "player@test.local" });

        existing.Name.ShouldBe("new");
        existing.PasswordHash.ShouldBe("old-hash");
        await _users.Received(1).UpdateAsync(existing, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveUserAsync_UpdateWithPassword_ReplacesHash()
    {
        var existing = new User { Id = Guid.NewGuid(), Name = "player", Email = "player@test.local", PasswordHash = "old-hash" };
        _users.GetByIdAsync(existing.Id, Arg.Any<CancellationToken>()).Returns(existing);

        await _service.SaveUserAsync(new UserViewModel { Id = existing.Id, Name = "player", Email = "player@test.local", Password = "new-secret-1" });

        PasswordHasher.Verify("new-secret-1", existing.PasswordHash).ShouldBeTrue();
    }

    [Fact]
    public async Task SetUserRolesAsync_LeavesExactlyRequestedRoles()
    {
        Role admin = new() { Id = 1, Title = "Admin" }, moderator = new() { Id = 2, Title = "Moderator" };
        var user = new User { Id = Guid.NewGuid(), Name = "player", Email = "player@test.local", Roles = [admin, moderator] };
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _roles.GetByIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>()).Returns((IReadOnlyList<Role>)[moderator]);

        var result = await _service.SetUserRolesAsync(user.Id, [moderator.Id]);

        user.Roles.ShouldBe([moderator]);
        result.Roles.Select(r => r.Title).ShouldBe(["Moderator"]);
        await _users.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
    }

    #endregion
}