using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ReReviewer.Security;

// Секция "Jwt" конфигурации. SigningKey в репозиторий не кладётся: для разработки —
// dotnet user-secrets set "Jwt:SigningKey" "...", на сервере — переменная окружения Jwt__SigningKey.
public class JwtOptions
{
    public const string SectionName = "Jwt";

    // HMAC-SHA256 требует ключ не короче 256 бит.
    public const int MinSigningKeyBytes = 32;

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SigningKey { get; set; } = string.Empty;
    public int LifetimeMinutes { get; set; } = 60;

    public SymmetricSecurityKey CreateSigningKey() =>
        new(Encoding.UTF8.GetBytes(SigningKey));
}