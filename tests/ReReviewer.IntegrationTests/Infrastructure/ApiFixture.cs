using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ReReviewer.Data.Entities;

namespace ReReviewer.IntegrationTests.Infrastructure;

// Своя база и приложение на прогон API-тестов, плюс три готовых пользователя: обычный, Moderator и Admin.
public sealed class ApiFixture : IAsyncLifetime
{
    public const string Password = "secret-123";

    public const string Anonymous = "anonymous";
    public const string User = "user";
    public const string Moderator = "moderator";
    public const string Admin = "admin";

    private readonly DatabaseFixture _database = new();
    private readonly Dictionary<string, string> _tokens = [];

    public ApiFactory Factory { get; private set; } = null!;

    public string ConnectionString => _database.ConnectionString;

    public async ValueTask InitializeAsync()
    {
        await _database.InitializeAsync();
        Factory = new ApiFactory(_database.ConnectionString, "Testing");

        using var client = Factory.CreateClient();
        foreach (var caller in new[] { User, Moderator, Admin })
            (await client.PostAsJsonAsync("api/auth/register", new { name = caller, email = EmailOf(caller), password = Password })).EnsureSuccessStatusCode();

        // Роли попадают в токен при входе, поэтому назначаем их до логина.
        await using (var db = _database.CreateContext())
        {
            db.Users.Single(u => u.Email == EmailOf(Admin)).Roles.Add(new Role { Title = "Admin" });
            db.Users.Single(u => u.Email == EmailOf(Moderator)).Roles.Add(new Role { Title = "Moderator" });
            await db.SaveChangesAsync();
        }

        foreach (var caller in new[] { User, Moderator, Admin })
            _tokens[caller] = await LoginAsync(client, EmailOf(caller));
    }

    public async ValueTask DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _database.DisposeAsync();
    }

    public string? TokenOf(string caller) =>
        _tokens.GetValueOrDefault(caller);

    public HttpClient CreateClient(string caller)
    {
        var client = Factory.CreateClient();
        if (TokenOf(caller) is { } token)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    public static async Task<string> LoginAsync(HttpClient client, string email, string password = Password)
    {
        using var response = await client.PostAsJsonAsync("api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
    }

    private static string EmailOf(string caller) =>
        $"{caller}@api.test.local";
}

[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<ApiFixture>
{
    public const string Name = "Api";
}