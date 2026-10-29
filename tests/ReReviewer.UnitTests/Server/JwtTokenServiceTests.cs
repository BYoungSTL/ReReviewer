using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using ReReviewer.Application.ViewModels;
using ReReviewer.Security;

namespace ReReviewer.UnitTests.Server;

public class JwtTokenServiceTests
{
    private readonly JwtOptions _options = new()
    {
        Issuer = "ReReviewer",
        Audience = "ReReviewer",
        SigningKey = NewKey(),
        LifetimeMinutes = 30
    };

    private readonly UserViewModel _user = new()
    {
        Id = Guid.NewGuid(),
        Name = "player",
        Email = "player@test.local",
        Roles = [new() { Id = 1, Title = "Admin" }, new() { Id = 2, Title = "Moderator" }]
    };

    [Fact]
    public async Task CreateToken_IsValidForSameKeyIssuerAndAudience()
    {
        var result = CreateService().CreateToken(_user);

        var validation = await new JsonWebTokenHandler().ValidateTokenAsync(result.Token, Parameters(_options.SigningKey));

        validation.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task CreateToken_IsRejectedWithAnotherKey()
    {
        var result = CreateService().CreateToken(_user);

        var validation = await new JsonWebTokenHandler().ValidateTokenAsync(result.Token, Parameters(NewKey()));

        validation.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void CreateToken_ContainsUserClaims()
    {
        var token = new JsonWebToken(CreateService().CreateToken(_user).Token);

        token.Subject.ShouldBe(_user.Id.ToString());
        token.GetClaim("name").Value.ShouldBe("player");
        token.GetClaim("email").Value.ShouldBe("player@test.local");
        token.Claims.Where(c => c.Type == "role").Select(c => c.Value).ShouldBe(["Admin", "Moderator"], ignoreOrder: true);
    }

    [Fact]
    public void CreateToken_ExpiresAfterConfiguredLifetime()
    {
        var result = CreateService().CreateToken(_user);
        var token = new JsonWebToken(result.Token);
        var expected = DateTime.UtcNow.AddMinutes(_options.LifetimeMinutes);

        result.ExpiresAt.ShouldBe(expected, TimeSpan.FromMinutes(1));
        token.ValidTo.ShouldBe(expected, TimeSpan.FromMinutes(1));
        result.User.ShouldBeSameAs(_user);
    }

    private JwtTokenService CreateService() =>
        new(Options.Create(_options));

    private TokenValidationParameters Parameters(string signingKey) =>
        new()
        {
            ValidIssuer = _options.Issuer,
            ValidAudience = _options.Audience,
            IssuerSigningKey = new JwtOptions { SigningKey = signingKey }.CreateSigningKey()
        };

    private static string NewKey() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
}