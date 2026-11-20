using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ReReviewer.IntegrationTests.Infrastructure;

namespace ReReviewer.IntegrationTests.Api;

// Свой профиль: имя, почта, пароль и аватарка. Каждый тест работает со своим новым пользователем.
[Collection(ApiCollection.Name)]
public class UserSettingsApiTests(ApiFixture api)
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4, 5, 6, 7, 8];

    [Fact]
    public async Task UpdateProfile_NameWithoutPassword_ReturnsTokenWithNewName()
    {
        var (_, email, client) = await NewUserAsync();

        using var response = await client.PutAsJsonAsync("api/users/me", new { name = "  Renamed  ", email });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var token = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
        Claims(token).GetProperty("name").GetString().ShouldBe("Renamed");
    }

    [Fact]
    public async Task UpdateProfile_Email_RequiresCorrectCurrentPassword()
    {
        var (_, _, client) = await NewUserAsync();
        var newEmail = $"{Guid.NewGuid():N}@test.local";

        await ShouldFailAsync(client.PutAsJsonAsync("api/users/me", new { name = "user", email = newEmail }),
            "Current password is required to change email.");
        await ShouldFailAsync(client.PutAsJsonAsync("api/users/me", new { name = "user", email = newEmail, currentPassword = "wrong-password" }),
            "Current password is incorrect.");

        using var changed = await client.PutAsJsonAsync("api/users/me", new { name = "user", email = newEmail, currentPassword = ApiFixture.Password });
        changed.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ApiFixture.LoginAsync(api.Factory.CreateClient(), newEmail)).ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task ChangePassword_EndsOtherSessions_AndKeepsThisOne()
    {
        var (_, email, client) = await NewUserAsync();
        using var otherSession = api.Factory.CreateClient();
        otherSession.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await ApiFixture.LoginAsync(otherSession, email));
        // iat в токене — с точностью до секунды: смена пароля должна случиться в следующую секунду после входа.
        await Task.Delay(TimeSpan.FromSeconds(1.1));

        await ShouldFailAsync(client.PutAsJsonAsync("api/users/me/password", new { currentPassword = "wrong-password", newPassword = "new-secret-123" }),
            "Current password is incorrect.");
        using var changed = await client.PutAsJsonAsync("api/users/me/password", new { currentPassword = ApiFixture.Password, newPassword = "new-secret-123" });
        changed.StatusCode.ShouldBe(HttpStatusCode.OK);
        var newToken = (await changed.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();

        (await client.GetAsync("api/auth/me")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await otherSession.GetAsync("api/auth/me")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
        (await client.GetAsync("api/auth/me")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ApiFixture.LoginAsync(api.Factory.CreateClient(), email, "new-secret-123")).ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Avatar_UploadedIsServedToEveryone_UntilRemoved()
    {
        var (id, _, client) = await NewUserAsync();

        using var uploaded = await client.PutAsync("api/users/me/avatar", AvatarForm(Png, "avatar.png"));
        uploaded.StatusCode.ShouldBe(HttpStatusCode.OK);
        var token = (await uploaded.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
        Claims(token).TryGetProperty("avatar", out _).ShouldBeTrue();

        using var anonymous = api.Factory.CreateClient();
        using var served = await anonymous.GetAsync($"api/users/{id}/avatar");
        served.StatusCode.ShouldBe(HttpStatusCode.OK);
        served.Content.Headers.ContentType!.MediaType.ShouldBe("image/png");
        served.Headers.GetValues("X-Content-Type-Options").ShouldBe(["nosniff"]);
        (await served.Content.ReadAsByteArrayAsync()).ShouldBe(Png);

        (await client.DeleteAsync("api/users/me/avatar")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await anonymous.GetAsync($"api/users/{id}/avatar")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Avatar_NotAnImage_Returns400()
    {
        var (_, _, client) = await NewUserAsync();

        await ShouldFailAsync(client.PutAsync("api/users/me/avatar", AvatarForm(Encoding.UTF8.GetBytes("<svg onload=alert(1)>"), "avatar.png")),
            "Avatar must be a PNG, JPEG or WebP image.");
    }

    private async Task<(string Id, string Email, HttpClient Client)> NewUserAsync()
    {
        var client = api.Factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@test.local";
        using var register = await client.PostAsJsonAsync("api/auth/register", new { name = "user", email, password = ApiFixture.Password });
        register.EnsureSuccessStatusCode();
        var id = (await register.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await ApiFixture.LoginAsync(client, email));

        return (id, email, client);
    }

    private static MultipartFormDataContent AvatarForm(byte[] content, string fileName) =>
        new() { { new ByteArrayContent(content), "file", fileName } };

    private static async Task ShouldFailAsync(Task<HttpResponseMessage> request, string detail)
    {
        using var response = await request;
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("detail").GetString().ShouldBe(detail);
    }

    private static JsonElement Claims(string token) =>
        JsonDocument.Parse(Convert.FromBase64String(Base64(token.Split('.')[1]))).RootElement;

    private static string Base64(string base64Url)
    {
        var base64 = base64Url.Replace('-', '+').Replace('_', '/');

        return base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');
    }
}
