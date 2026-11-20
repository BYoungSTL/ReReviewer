using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ReReviewer.IntegrationTests.Infrastructure;

namespace ReReviewer.IntegrationTests.Api;

// Публичная страница пользователя: api/users/{id}/profile и api/users/{id}/reviews открыты всем.
[Collection(ApiCollection.Name)]
public class PublicUserApiTests(ApiFixture api)
{
    // Привязанный аккаунт виден под именем своего профиля обзорщика, а не под именем аккаунта.
    [Fact]
    public async Task Profile_ShowsReviewerNameAndLinkedReviewer_WithoutPrivateData()
    {
        var reviewerName = TestData.Unique("Reviewer");
        var reviewerId = await CreateAsync("api/reviewers", new { fullName = reviewerName, genres = new[] { new { id = int.Parse(await GenreAsync()) } } });
        var (userId, _) = await RegisterAsync("Публичный автор");
        using (var moderator = api.CreateClient(ApiFixture.Moderator))
            (await moderator.PostAsync($"api/reviewers/{reviewerId}/validate/{userId}", null)).EnsureSuccessStatusCode();

        using var anonymous = api.CreateClient(ApiFixture.Anonymous);
        var profile = await anonymous.GetFromJsonAsync<JsonElement>($"api/users/{userId}/profile");

        profile.GetProperty("id").GetString().ShouldBe(userId);
        profile.GetProperty("name").GetString().ShouldBe(reviewerName);
        profile.GetProperty("reviewerId").GetString().ShouldBe(reviewerId);
        profile.GetProperty("avatarUpdatedAt").ValueKind.ShouldBe(JsonValueKind.Null);
        profile.GetProperty("createdAt").GetString().ShouldEndWith("Z");
        profile.TryGetProperty("email", out _).ShouldBeFalse();
        profile.TryGetProperty("roles", out _).ShouldBeFalse();
        profile.TryGetProperty("blockedAt", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task Profile_UnlinkedUser_ShowsAccountName()
    {
        var (userId, _) = await RegisterAsync("Просто читатель");
        using var anonymous = api.CreateClient(ApiFixture.Anonymous);

        var profile = await anonymous.GetFromJsonAsync<JsonElement>($"api/users/{userId}/profile");

        profile.GetProperty("name").GetString().ShouldBe("Просто читатель");
        profile.GetProperty("reviewerId").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Profile_UnknownUser_Returns404()
    {
        using var anonymous = api.CreateClient(ApiFixture.Anonymous);

        (await anonymous.GetAsync($"api/users/{Guid.NewGuid()}/profile")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Reviews_ReturnsOnlyPublished()
    {
        var genre = int.Parse(await GenreAsync());
        var published = await CreateAsync("api/reviewers", new { fullName = TestData.Unique("Reviewer"), genres = new[] { new { id = genre } } });
        var pending = await CreateAsync("api/reviewers", new { fullName = TestData.Unique("Reviewer"), genres = new[] { new { id = genre } } });
        var (userId, author) = await RegisterAsync("Автор отзывов");

        var publishedReview = await PostReviewAsync(author, published, "Опубликованный");
        await PostReviewAsync(author, pending, "На модерации");
        using (var moderator = api.CreateClient(ApiFixture.Moderator))
            (await moderator.PostAsync($"api/reviews/{publishedReview}/approve", null)).EnsureSuccessStatusCode();

        using var anonymous = api.CreateClient(ApiFixture.Anonymous);
        var page = await anonymous.GetFromJsonAsync<JsonElement>($"api/users/{userId}/reviews");

        page.GetProperty("totalCount").GetInt32().ShouldBe(1);
        var review = page.GetProperty("items").EnumerateArray().Single();
        review.GetProperty("id").GetString().ShouldBe(publishedReview);
        review.GetProperty("reviewerId").GetString().ShouldBe(published);
        review.GetProperty("status").GetString().ShouldBe("Published");
    }

    private async Task<(string UserId, HttpClient Client)> RegisterAsync(string name)
    {
        var client = api.Factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@test.local";
        using var register = await client.PostAsJsonAsync("api/auth/register", new { name, email, password = ApiFixture.Password });
        register.EnsureSuccessStatusCode();
        var userId = (await register.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await ApiFixture.LoginAsync(client, email));

        return (userId, client);
    }

    private static async Task<string> PostReviewAsync(HttpClient author, string reviewerId, string text)
    {
        using var response = await author.PostAsJsonAsync("api/reviews", new { reviewerId, text, score = 7 });
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;
    }

    private Task<string> GenreAsync() =>
        CreateAsync("api/genres", new { title = TestData.Unique("Genre") });

    private async Task<string> CreateAsync(string url, object body)
    {
        using var admin = api.CreateClient(ApiFixture.Admin);
        using var response = await admin.PostAsJsonAsync(url, body);
        response.EnsureSuccessStatusCode();

        var id = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id");

        return id.ValueKind == JsonValueKind.Number ? id.GetInt32().ToString() : id.GetString()!;
    }
}
