using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using ReReviewer.Application.ViewModels;

namespace ReReviewer.Security;

// Выпускает JWT, подписанный HMAC-SHA256. Claims: sub — Id пользователя, name, email и по claim role
// на каждую роль. Короткие имена claims совпадают с NameClaimType/RoleClaimType в Startup.
public class JwtTokenService(IOptions<JwtOptions> options) : IJwtTokenService
{
    public AuthTokenViewModel CreateToken(UserViewModel user)
    {
        var jwt = options.Value;
        var expiresAt = DateTime.UtcNow.AddMinutes(jwt.LifetimeMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id!.Value.ToString()),
            new(JwtRegisteredClaimNames.Name, user.Name),
            new(JwtRegisteredClaimNames.Email, user.Email)
        };
        claims.AddRange(user.Roles.Select(role => new Claim("role", role.Title)));

        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(jwt.CreateSigningKey(), SecurityAlgorithms.HmacSha256)
        });

        return new AuthTokenViewModel
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = user
        };
    }
}