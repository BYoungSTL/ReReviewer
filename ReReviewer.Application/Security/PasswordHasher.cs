using System.Security.Cryptography;

namespace ReReviewer.Application.Security;

// PBKDF2-HMAC-SHA512 из стандартной библиотеки. Формат хэша: "итерации.соль.хэш" (соль и хэш в Base64),
// число итераций хранится в самом хэше, поэтому его можно поднять позже без пересчёта старых паролей.
public static class PasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 64;
    private const int Iterations = 210_000;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA512, HashSize);

        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    // Повреждённый хэш в базе считается неподошедшим паролем: вход должен ответить 401, а не 500.
    public static bool Verify(string password, string passwordHash)
    {
        var parts = passwordHash.Split('.');
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations) || iterations <= 0)
            return false;

        if (!TryFromBase64(parts[1], out var salt) || !TryFromBase64(parts[2], out var expected) || expected.Length == 0)
            return false;

        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA512, expected.Length);

        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static bool TryFromBase64(string value, out byte[] bytes)
    {
        var buffer = new byte[value.Length];
        var ok = Convert.TryFromBase64String(value, buffer, out var written);
        bytes = buffer[..written];

        return ok;
    }
}