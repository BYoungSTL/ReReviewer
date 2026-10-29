using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ReReviewer.IntegrationTests.Infrastructure;

namespace ReReviewer.IntegrationTests.Api;

[Collection(ApiCollection.Name)]
public class AuthApiTests(ApiFixture api)
{
    [Fact]
    public async Task Register_Login_Me_ReturnsSameUser()
    {
        using var client = api.Factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@test.local";

        using var register = await client.PostAsJsonAsync("api/auth/register", new { name = "new", email, password = ApiFixture.Password });
        register.StatusCode.ShouldBe(HttpStatusCode.Created);
        var userId = (await register.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();

        var token = await ApiFixture.LoginAsync(client, email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var me = await client.GetFromJsonAsync<JsonElement>("api/auth/me");

        me.GetProperty("id").GetString().ShouldBe(userId);
        me.GetProperty("email").GetString().ShouldBe(email);
    }

    [Fact]
    public async Task Register_ResponseDoesNotContainPassword()
    {
        using var client = api.Factory.CreateClient();

        using var response = await client.PostAsJsonAsync("api/auth/register", new { name = "new", email = $"{Guid.NewGuid():N}@test.local", password = ApiFixture.Password });
        var body = await response.Content.ReadAsStringAsync();

        body.ShouldNotContain(ApiFixture.Password);
        body.ShouldNotContain("passwordHash", Case.Insensitive);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns400()
    {
        using var client = api.Factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@test.local";
        (await client.PostAsJsonAsync("api/auth/register", new { name = "first", email, password = ApiFixture.Password })).EnsureSuccessStatusCode();

        using var response = await client.PostAsJsonAsync("api/auth/register", new { name = "second", email = email.ToUpperInvariant(), password = ApiFixture.Password });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("detail").GetString().ShouldBe("Email is already in use.");
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401ProblemDetails()
    {
        using var client = api.Factory.CreateClient();

        using var response = await client.PostAsJsonAsync("api/auth/login", new { email = "user@api.test.local", password = "wrong-password" });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("detail").GetString().ShouldBe("Invalid email or password.");
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        using var client = api.Factory.CreateClient();

        (await client.GetAsync("api/auth/me")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithTamperedSignature_Returns401()
    {
        using var client = api.Factory.CreateClient();
        var token = api.TokenOf(ApiFixture.User)!;
        var tampered = token[..^2] + (token[^2] == 'A' ? "B" : "A") + token[^1];
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tampered);

        (await client.GetAsync("api/auth/me")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithTokenFromAnotherKey_Returns401()
    {
        await using var otherApp = new ApiFactory(api.ConnectionString, "Testing");
        var foreignToken = await ApiFixture.LoginAsync(otherApp.CreateClient(), "user@api.test.local");
        using var client = api.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", foreignToken);

        (await client.GetAsync("api/auth/me")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}