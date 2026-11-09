using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ReReviewer.IntegrationTests.Infrastructure;

namespace ReReviewer.IntegrationTests.Api;

// Обзорщик с подтверждённым аккаунтом правит свой профиль через PUT api/users/me/reviewer.
[Collection(ApiCollection.Name)]
public class ReviewerProfileApiTests(ApiFixture api)
{
    [Fact]
    public async Task UpdateMyReviewer_SavesProfile_AndReplacesOutlets()
    {
        var (genre1, genre2) = (await CreateAsync("api/genres", new { title = TestData.Unique("Genre") }), await CreateAsync("api/genres", new { title = TestData.Unique("Genre") }));
        var (outlet1, outlet2) = (await CreateAsync("api/outlets", new { name = TestData.Unique("Outlet") }), await CreateAsync("api/outlets", new { name = TestData.Unique("Outlet") }));
        var (reviewerId, owner) = await LinkedReviewerAsync(genre1);
        var name = TestData.Unique("Renamed");

        using var saved = await owner.PutAsJsonAsync("api/users/me/reviewer", new
        {
            fullName = $"  {name}  ",
            about = "  Пишу про стратегии  ",
            links = new[] { " https://example.com/channel ", "", "https://EXAMPLE.com/channel", "https://t.me/example" },
            genres = new[] { new { id = int.Parse(genre2) } },
            outlets = new[] { new { outletId = outlet1, startedAt = "2019-05-01", endedAt = (string?)null } }
        });
        saved.StatusCode.ShouldBe(HttpStatusCode.OK);

        var reviewer = await owner.GetFromJsonAsync<JsonElement>($"api/reviewers/{reviewerId}");
        reviewer.GetProperty("fullName").GetString().ShouldBe(name);
        reviewer.GetProperty("about").GetString().ShouldBe("Пишу про стратегии");
        reviewer.GetProperty("links").EnumerateArray().Select(l => l.GetString()).ShouldBe(["https://example.com/channel", "https://t.me/example"]);
        reviewer.GetProperty("genres").EnumerateArray().Select(g => g.GetProperty("id").GetInt32().ToString()).ShouldBe([genre2]);
        var outlet = reviewer.GetProperty("outlets").EnumerateArray().Single();
        outlet.GetProperty("outletId").GetString().ShouldBe(outlet1);
        outlet.GetProperty("startedAt").GetString().ShouldBe("2019-05-01");

        using var replaced = await owner.PutAsJsonAsync("api/users/me/reviewer", new
        {
            fullName = name,
            genres = new[] { new { id = int.Parse(genre2) } },
            outlets = new[] { new { outletId = outlet2, startedAt = (string?)null, endedAt = (string?)null } }
        });
        replaced.StatusCode.ShouldBe(HttpStatusCode.OK);
        var outlets = (await owner.GetFromJsonAsync<JsonElement>($"api/reviewers/{reviewerId}")).GetProperty("outlets");
        outlets.EnumerateArray().Select(o => o.GetProperty("outletId").GetString()).ShouldBe([outlet2]);
    }

    [Fact]
    public async Task UpdateMyReviewer_WithoutLinkedProfile_Returns404()
    {
        var genre = await CreateAsync("api/genres", new { title = TestData.Unique("Genre") });
        using var user = api.CreateClient(ApiFixture.User);

        using var response = await user.PutAsJsonAsync("api/users/me/reviewer", new { fullName = "Me", genres = new[] { new { id = int.Parse(genre) } } });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("detail").GetString()
            .ShouldBe("Your account is not linked to a reviewer profile.");
    }

    [Theory]
    [InlineData("name", "A reviewer with this name already exists.")]
    [InlineData("link", "Links must be http or https addresses.")]
    [InlineData("dates", "An outlet's end date cannot be earlier than its start date.")]
    public async Task UpdateMyReviewer_InvalidProfile_Returns400(string problem, string detail)
    {
        var genre = await CreateAsync("api/genres", new { title = TestData.Unique("Genre") });
        var outlet = await CreateAsync("api/outlets", new { name = TestData.Unique("Outlet") });
        var otherName = TestData.Unique("Taken");
        await CreateAsync("api/reviewers", new { fullName = otherName, genres = new[] { new { id = int.Parse(genre) } } });
        var (_, owner) = await LinkedReviewerAsync(genre);

        using var response = await owner.PutAsJsonAsync("api/users/me/reviewer", new
        {
            fullName = problem == "name" ? otherName.ToLowerInvariant() : TestData.Unique("Own"),
            links = problem == "link" ? new[] { "javascript:alert(1)" } : [],
            genres = new[] { new { id = int.Parse(genre) } },
            outlets = problem == "dates"
                ? new[] { new { outletId = outlet, startedAt = (string?)"2022-01-01", endedAt = (string?)"2021-01-01" } }
                : []
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("detail").GetString().ShouldBe(detail);
    }

    // Новый обзорщик, привязанный модератором к новому пользователю; клиент — от имени этого пользователя.
    private async Task<(string ReviewerId, HttpClient Owner)> LinkedReviewerAsync(string genreId)
    {
        var reviewerId = await CreateAsync("api/reviewers", new { fullName = TestData.Unique("Reviewer"), genres = new[] { new { id = int.Parse(genreId) } } });

        var owner = api.Factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@test.local";
        using var register = await owner.PostAsJsonAsync("api/auth/register", new { name = "owner", email, password = ApiFixture.Password });
        register.EnsureSuccessStatusCode();
        var userId = (await register.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();

        using var moderator = api.CreateClient(ApiFixture.Moderator);
        (await moderator.PostAsync($"api/reviewers/{reviewerId}/validate/{userId}", null)).EnsureSuccessStatusCode();

        owner.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await ApiFixture.LoginAsync(owner, email));

        return (reviewerId, owner);
    }

    private async Task<string> CreateAsync(string url, object body)
    {
        using var admin = api.CreateClient(ApiFixture.Admin);
        using var response = await admin.PostAsJsonAsync(url, body);
        response.EnsureSuccessStatusCode();

        var id = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id");

        return id.ValueKind == JsonValueKind.Number ? id.GetInt32().ToString() : id.GetString()!;
    }
}
