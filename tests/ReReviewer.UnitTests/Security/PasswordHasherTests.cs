using ReReviewer.Application.Security;

namespace ReReviewer.UnitTests.Security;

public class PasswordHasherTests
{
    private const string Password = "correct horse battery";

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue() =>
        PasswordHasher.Verify(Password, PasswordHasher.Hash(Password)).ShouldBeTrue();

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse() =>
        PasswordHasher.Verify("wrong password", PasswordHasher.Hash(Password)).ShouldBeFalse();

    [Fact]
    public void Hash_SamePasswordTwice_UsesDifferentSalts() =>
        PasswordHasher.Hash(Password).ShouldNotBe(PasswordHasher.Hash(Password));

    [Fact]
    public void Hash_StoresIterationsSaltAndHash()
    {
        var parts = PasswordHasher.Hash(Password).Split('.');

        parts.Length.ShouldBe(3);
        int.Parse(parts[0]).ShouldBeGreaterThanOrEqualTo(100_000);
        Convert.FromBase64String(parts[1]).ShouldNotBeEmpty();
        Convert.FromBase64String(parts[2]).ShouldNotBeEmpty();
    }

    // Повреждённый хэш в базе — это «пароль не подошёл», а не исключение.
    [Theory]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData("abc.c2FsdA==.aGFzaA==")]
    [InlineData("0.c2FsdA==.aGFzaA==")]
    [InlineData("210000.!!!.???")]
    [InlineData("210000.c2FsdA==.")]
    [InlineData("210000.c2FsdA==")]
    public void Verify_MalformedHash_ReturnsFalse(string hash) =>
        PasswordHasher.Verify(Password, hash).ShouldBeFalse();
}