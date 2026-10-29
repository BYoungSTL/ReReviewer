using System.Net.Http.Json;
using ReReviewer.IntegrationTests.Infrastructure;

namespace ReReviewer.IntegrationTests.Api;

// Матрица прав: каждый эндпоинт × аноним, пользователь, Moderator, Admin.
// Проверяется только авторизация: несуществующие Id и пустое тело дают 400/404, но не 401/403,
// если доступ есть, — и ничего не создают.
[Collection(ApiCollection.Name)]
public class AccessRulesTests(ApiFixture api)
{
    private const string Everyone = "everyone";
    private const string SignedIn = "signed-in";
    private const string Staff = "staff";
    private const string AdminOnly = "admin";

    private const string G = "8b0c6c2e-5f2d-4b8e-9a51-3c1d7e0f4a92";
    private const string I = "999999";

    private static readonly (string Method, string Url, string Access)[] Endpoints =
    [
        ("GET", "api/genres", Everyone), ("GET", $"api/genres/{I}", Everyone),
        ("POST", "api/genres", Staff), ("PUT", $"api/genres/{I}", Staff), ("DELETE", $"api/genres/{I}", Staff),

        ("GET", "api/badges", Everyone), ("GET", $"api/badges/{I}", Everyone),
        ("POST", "api/badges", Staff), ("PUT", $"api/badges/{I}", Staff), ("DELETE", $"api/badges/{I}", Staff),

        ("GET", "api/criteria", Everyone), ("GET", $"api/criteria/{I}", Everyone),
        ("POST", "api/criteria", Staff), ("PUT", $"api/criteria/{I}", Staff), ("DELETE", $"api/criteria/{I}", Staff),

        ("GET", "api/roles", AdminOnly), ("GET", $"api/roles/{I}", AdminOnly),
        ("POST", "api/roles", AdminOnly), ("PUT", $"api/roles/{I}", AdminOnly), ("DELETE", $"api/roles/{I}", AdminOnly),

        ("GET", "api/games", Everyone), ("GET", $"api/games/{G}", Everyone), ("GET", $"api/games/{G}/materials", Everyone),
        ("POST", "api/games", Staff), ("PUT", $"api/games/{G}", Staff), ("DELETE", $"api/games/{G}", Staff),

        ("GET", "api/outlets", Everyone), ("GET", $"api/outlets/{G}", Everyone),
        ("POST", "api/outlets", Staff), ("PUT", $"api/outlets/{G}", Staff), ("DELETE", $"api/outlets/{G}", Staff),
        ("POST", $"api/outlets/{G}/reviewers", Staff),

        ("GET", "api/reviewers", Everyone), ("GET", $"api/reviewers/{G}", Everyone),
        ("GET", $"api/reviewers/{G}/materials", Everyone), ("GET", $"api/reviewers/{G}/reviews", Everyone),
        ("GET", $"api/reviewers/{G}/rating", Everyone),
        ("POST", "api/reviewers", Staff), ("PUT", $"api/reviewers/{G}", Staff), ("DELETE", $"api/reviewers/{G}", Staff),
        ("POST", $"api/reviewers/{G}/validate/{G}", Staff), ("POST", $"api/reviewers/{G}/materials", Staff),

        ("GET", $"api/materials/{G}", Everyone),
        ("POST", "api/materials", Staff), ("PUT", $"api/materials/{G}", Staff), ("DELETE", $"api/materials/{G}", Staff),

        ("GET", $"api/reviews/{G}", Everyone),
        ("GET", "api/reviews/my", SignedIn), ("POST", "api/reviews", SignedIn),
        ("PUT", $"api/reviews/{G}", SignedIn), ("DELETE", $"api/reviews/{G}", SignedIn),
        ("GET", "api/reviews/moderation", Staff), ("POST", $"api/reviews/{G}/approve", Staff), ("POST", $"api/reviews/{G}/reject", Staff),

        ("PUT", "api/users/me", SignedIn),
        ("GET", "api/users", AdminOnly), ("GET", $"api/users/{G}", AdminOnly), ("POST", "api/users", AdminOnly),
        ("PUT", $"api/users/{G}", AdminOnly), ("DELETE", $"api/users/{G}", AdminOnly), ("PUT", $"api/users/{G}/roles", AdminOnly),

        ("GET", "api/auth/me", SignedIn)
    ];

    public static TheoryData<string, string, string, string> Cases()
    {
        var cases = new TheoryData<string, string, string, string>();
        foreach (var caller in new[] { ApiFixture.Anonymous, ApiFixture.User, ApiFixture.Moderator, ApiFixture.Admin })
            foreach (var (method, url, access) in Endpoints)
                cases.Add(caller, method, url, access);

        return cases;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Endpoint_AllowsOnlyPermittedCallers(string caller, string method, string url, string access)
    {
        using var client = api.CreateClient(caller);
        using var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (method is "POST" or "PUT")
            request.Content = JsonContent.Create(new { });

        using var response = await client.SendAsync(request);
        var status = (int)response.StatusCode;

        if (IsAllowed(caller, access))
            status.ShouldNotBeOneOf([401, 403], $"{caller} должен иметь доступ к {method} {url}");
        else
            status.ShouldBe(caller == ApiFixture.Anonymous ? 401 : 403, $"{caller} не должен иметь доступ к {method} {url}");
    }

    private static bool IsAllowed(string caller, string access) =>
        access switch
        {
            Everyone => true,
            SignedIn => caller != ApiFixture.Anonymous,
            Staff => caller is ApiFixture.Moderator or ApiFixture.Admin,
            _ => caller == ApiFixture.Admin
        };
}