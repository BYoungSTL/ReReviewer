using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ReReviewer.IntegrationTests.Infrastructure;

namespace ReReviewer.IntegrationTests.Api;

// Заблокированный пользователь не входит, а выданный ему токен перестаёт действовать сразу, не дожидаясь срока.
[Collection(ApiCollection.Name)]
public class UserBlockingApiTests(ApiFixture api)
{
    [Fact]
    public async Task Block_RejectsLoginAndIssuedToken_UntilUnblocked()
    {
        var (id, email) = await RegisterAsync();
        using var user = await SignedInClientAsync(email);
        using var anonymous = api.Factory.CreateClient();
        using var admin = api.CreateClient(ApiFixture.Admin);
        (await user.GetAsync("api/auth/me")).StatusCode.ShouldBe(HttpStatusCode.OK);

        using var block = await admin.PostAsync($"api/users/{id}/block", null);
        block.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await block.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("blockedAt").ValueKind.ShouldBe(JsonValueKind.String);

        (await user.GetAsync("api/auth/me")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        using var login = await anonymous.PostAsJsonAsync("api/auth/login", new { email, password = ApiFixture.Password });
        login.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("detail").GetString().ShouldBe("This account is blocked.");

        using var unblock = await admin.PostAsync($"api/users/{id}/unblock", null);
        unblock.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await unblock.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("blockedAt").ValueKind.ShouldBe(JsonValueKind.Null);
        (await user.GetAsync("api/auth/me")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_BlockedUserWithWrongPassword_DoesNotRevealBlock()
    {
        var (id, email) = await RegisterAsync();
        using var admin = api.CreateClient(ApiFixture.Admin);
        (await admin.PostAsync($"api/users/{id}/block", null)).EnsureSuccessStatusCode();
        using var anonymous = api.Factory.CreateClient();

        using var login = await anonymous.PostAsJsonAsync("api/auth/login", new { email, password = "wrong-password" });

        login.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("detail").GetString().ShouldBe("Invalid email or password.");
    }

    [Fact]
    public async Task Block_Self_Returns400()
    {
        using var admin = api.CreateClient(ApiFixture.Admin);
        var me = await admin.GetFromJsonAsync<JsonElement>("api/auth/me");

        using var response = await admin.PostAsync($"api/users/{me.GetProperty("id").GetString()}/block", null);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("detail").GetString().ShouldBe("You cannot block yourself.");
    }

    [Fact]
    public async Task Delete_IssuedTokenStopsWorking()
    {
        var (id, email) = await RegisterAsync();
        using var user = await SignedInClientAsync(email);
        using var admin = api.CreateClient(ApiFixture.Admin);

        (await admin.DeleteAsync($"api/users/{id}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await user.GetAsync("api/auth/me")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private async Task<(string Id, string Email)> RegisterAsync()
    {
        using var client = api.Factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@test.local";

        using var response = await client.PostAsJsonAsync("api/auth/register", new { name = "blocked", email, password = ApiFixture.Password });
        response.EnsureSuccessStatusCode();

        return ((await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!, email);
    }

    private async Task<HttpClient> SignedInClientAsync(string email)
    {
        var client = api.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await ApiFixture.LoginAsync(client, email));

        return client;
    }
}
