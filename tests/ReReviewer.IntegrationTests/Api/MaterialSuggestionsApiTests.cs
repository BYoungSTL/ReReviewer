using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ReReviewer.IntegrationTests.Infrastructure;

namespace ReReviewer.IntegrationTests.Api;

// Читатель предлагает материал обзорщика, модератор одобряет — материал появляется у обзорщика.
[Collection(ApiCollection.Name)]
public class MaterialSuggestionsApiTests(ApiFixture api)
{
    [Fact]
    public async Task SuggestedMaterial_AppearsAtReviewerOnlyAfterApproval()
    {
        var genre = await CreateAsync("api/genres", new { title = TestData.Unique("Genre") });
        var reviewerId = await CreateAsync("api/reviewers", new { fullName = TestData.Unique("Reviewer"), genres = new[] { new { id = int.Parse(genre) } } });
        var gameId = await CreateAsync("api/games", new { title = TestData.Unique("Game"), genres = new[] { new { id = int.Parse(genre) } } });
        var url = $"https://example.com/{Guid.NewGuid():N}";
        var reader = await ReaderAsync();

        using var created = await reader.PostAsJsonAsync("api/materialsuggestions",
            new { reviewerId, gameId, title = "  Обзор от читателя  ", url, format = "Video", publishedAt = "2026-11-01" });
        created.StatusCode.ShouldBe(HttpStatusCode.Created);
        var suggestion = await created.Content.ReadFromJsonAsync<JsonElement>();
        suggestion.GetProperty("title").GetString().ShouldBe("Обзор от читателя");
        suggestion.GetProperty("status").GetString().ShouldBe("Pending");
        var suggestionId = suggestion.GetProperty("id").GetString();

        using var anonymous = api.CreateClient(ApiFixture.Anonymous);
        (await anonymous.GetFromJsonAsync<JsonElement>($"api/reviewers/{reviewerId}/materials")).GetArrayLength().ShouldBe(0);

        using var moderator = api.CreateClient(ApiFixture.Moderator);
        var queue = await moderator.GetFromJsonAsync<JsonElement>("api/materialsuggestions/moderation?pageSize=100");
        queue.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetString()).ShouldContain(suggestionId);
        (await moderator.PostAsync($"api/materialsuggestions/{suggestionId}/approve", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var materials = await anonymous.GetFromJsonAsync<JsonElement>($"api/reviewers/{reviewerId}/materials");
        materials.EnumerateArray().Single().GetProperty("url").GetString().ShouldBe(url);
        var mine = await reader.GetFromJsonAsync<JsonElement>("api/materialsuggestions/my");
        mine.EnumerateArray().Single().GetProperty("status").GetString().ShouldBe("Approved");
    }

    // Материал, добавленный в отзыв, проверяется вместе с ним: одобрили отзыв — материал у обзорщика и отмечен в отзыве.
    [Fact]
    public async Task MaterialFromReview_IsApprovedTogetherWithReview()
    {
        var genre = await CreateAsync("api/genres", new { title = TestData.Unique("Genre") });
        var reviewerId = await CreateAsync("api/reviewers", new { fullName = TestData.Unique("Reviewer"), genres = new[] { new { id = int.Parse(genre) } } });
        var gameId = await CreateAsync("api/games", new { title = TestData.Unique("Game"), genres = new[] { new { id = int.Parse(genre) } } });
        var url = $"https://example.com/{Guid.NewGuid():N}";
        var reader = await ReaderAsync();

        using var created = await reader.PostAsJsonAsync("api/reviews", new
        {
            reviewerId,
            text = "Хороший разбор",
            score = 8,
            newMaterials = new[] { new { gameId, title = " Разбор из отзыва ", url, format = "Article" } }
        });
        created.StatusCode.ShouldBe(HttpStatusCode.Created);
        var reviewId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();

        var mine = await reader.GetFromJsonAsync<JsonElement>($"api/reviews/{reviewId}");
        var pending = mine.GetProperty("newMaterials").EnumerateArray().Single();
        pending.GetProperty("title").GetString().ShouldBe("Разбор из отзыва");
        pending.GetProperty("status").GetString().ShouldBe("Pending");

        using var moderator = api.CreateClient(ApiFixture.Moderator);
        var queue = await moderator.GetFromJsonAsync<JsonElement>("api/materialsuggestions/moderation?pageSize=100");
        queue.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("url").GetString()).ShouldNotContain(url);
        (await moderator.PostAsync($"api/reviews/{reviewId}/approve", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var anonymous = api.CreateClient(ApiFixture.Anonymous);
        var material = (await anonymous.GetFromJsonAsync<JsonElement>($"api/reviewers/{reviewerId}/materials")).EnumerateArray().Single();
        material.GetProperty("url").GetString().ShouldBe(url);
        var review = await anonymous.GetFromJsonAsync<JsonElement>($"api/reviews/{reviewId}");
        review.GetProperty("materials").EnumerateArray().Single().GetProperty("id").GetString().ShouldBe(material.GetProperty("id").GetString());
        review.GetProperty("newMaterials").GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task Suggestion_WithScriptLink_Returns400()
    {
        var reader = await ReaderAsync();

        using var response = await reader.PostAsJsonAsync("api/materialsuggestions",
            new { reviewerId = Guid.NewGuid(), gameId = Guid.NewGuid(), title = "x", url = "javascript:alert(1)", format = "Video" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("detail").GetString()
            .ShouldBe("Material link must be an http or https address.");
    }

    private async Task<HttpClient> ReaderAsync()
    {
        var client = api.Factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@test.local";
        (await client.PostAsJsonAsync("api/auth/register", new { name = "reader", email, password = ApiFixture.Password })).EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await ApiFixture.LoginAsync(client, email));

        return client;
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
