using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ReReviewer.IntegrationTests.Infrastructure;

namespace ReReviewer.IntegrationTests.Api;

// Заявки на новых обзорщиков и запросы на привязку профиля: от отправки до решения модератора.
[Collection(ApiCollection.Name)]
public class ReviewerRequestsApiTests(ApiFixture api)
{
    #region Suggestions

    [Fact]
    public async Task Suggestion_Approved_CreatesReviewerLinkedToAuthor_WhenSelf()
    {
        var genreId = await CreateGenreAsync();
        var (userId, user) = await NewUserAsync();
        var name = TestData.Unique("Suggested");

        using var created = await user.PostAsJsonAsync("api/suggestions",
            new { fullName = $"  {name}  ", note = "https://example.com/channel", isSelf = true, genres = new[] { new { id = genreId } } });
        created.StatusCode.ShouldBe(HttpStatusCode.Created);
        var suggestion = await created.Content.ReadFromJsonAsync<JsonElement>();
        suggestion.GetProperty("status").GetString().ShouldBe("Pending");
        suggestion.GetProperty("fullName").GetString().ShouldBe(name);
        var id = suggestion.GetProperty("id").GetString();

        using var moderator = api.CreateClient(ApiFixture.Moderator);
        var queue = await moderator.GetFromJsonAsync<JsonElement>("api/suggestions/moderation?pageSize=100");
        queue.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetString()).ShouldContain(id);
        (await moderator.PostAsync($"api/suggestions/{id}/approve", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var mine = (await user.GetFromJsonAsync<JsonElement>("api/suggestions/my")).EnumerateArray().Single();
        mine.GetProperty("status").GetString().ShouldBe("Approved");
        var reviewer = await user.GetFromJsonAsync<JsonElement>($"api/reviewers/{mine.GetProperty("reviewerId").GetString()}");
        reviewer.GetProperty("fullName").GetString().ShouldBe(name);
        reviewer.GetProperty("userId").GetString().ShouldBe(userId);
        reviewer.GetProperty("genres").EnumerateArray().Single().GetProperty("id").GetInt32().ShouldBe(genreId);
    }

    [Fact]
    public async Task Suggestion_Rejected_CreatesNoReviewer()
    {
        var genreId = await CreateGenreAsync();
        var (_, user) = await NewUserAsync();
        var name = TestData.Unique("Rejected");
        var id = await SuggestAsync(user, name, genreId);

        using var moderator = api.CreateClient(ApiFixture.Moderator);
        (await moderator.PostAsync($"api/suggestions/{id}/reject", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var mine = (await user.GetFromJsonAsync<JsonElement>("api/suggestions/my")).EnumerateArray().Single();
        mine.GetProperty("status").GetString().ShouldBe("Rejected");
        mine.GetProperty("reviewerId").ValueKind.ShouldBe(JsonValueKind.Null);
        var reviewers = await user.GetFromJsonAsync<JsonElement>("api/reviewers");
        reviewers.EnumerateArray().Select(r => r.GetProperty("fullName").GetString()).ShouldNotContain(name);

        using var again = await moderator.PostAsync($"api/suggestions/{id}/approve", null);
        again.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Suggestion_NameOfExistingReviewer_Returns400()
    {
        var genreId = await CreateGenreAsync();
        var name = TestData.Unique("Existing");
        await CreateReviewerAsync(name, genreId);
        var (_, user) = await NewUserAsync();

        using var response = await user.PostAsJsonAsync("api/suggestions",
            new { fullName = name.ToUpperInvariant(), genres = new[] { new { id = genreId } } });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("detail").GetString()
            .ShouldBe("A reviewer with this name already exists.");
    }

    #endregion

    #region Claims

    [Fact]
    public async Task Claim_Approved_LinksProfile_AndRejectsCompetingClaims()
    {
        var reviewerId = await CreateReviewerAsync(TestData.Unique("Claimed"), await CreateGenreAsync());
        var (firstId, first) = await NewUserAsync();
        var (_, second) = await NewUserAsync();
        var claimId = await ClaimAsync(first, reviewerId);
        await ClaimAsync(second, reviewerId);

        using var moderator = api.CreateClient(ApiFixture.Moderator);
        (await moderator.PostAsync($"api/claims/{claimId}/approve", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await first.GetFromJsonAsync<JsonElement>($"api/reviewers/{reviewerId}")).GetProperty("userId").GetString().ShouldBe(firstId);
        var competing = (await second.GetFromJsonAsync<JsonElement>("api/claims/my")).EnumerateArray().Single();
        competing.GetProperty("status").GetString().ShouldBe("Rejected");

        using var late = await second.PostAsJsonAsync("api/claims", new { reviewerId, note = "It is me" });
        late.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await late.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("detail").GetString()
            .ShouldBe("This reviewer profile is already linked to a user.");
    }

    [Fact]
    public async Task Claim_WhileAnotherRequestIsPending_Returns400()
    {
        var genreId = await CreateGenreAsync();
        var first = await CreateReviewerAsync(TestData.Unique("First"), genreId);
        var second = await CreateReviewerAsync(TestData.Unique("Second"), genreId);
        var (_, user) = await NewUserAsync();
        await ClaimAsync(user, first);

        using var claim = await user.PostAsJsonAsync("api/claims", new { reviewerId = second, note = "It is me" });
        using var self = await user.PostAsJsonAsync("api/suggestions",
            new { fullName = TestData.Unique("Self"), isSelf = true, genres = new[] { new { id = genreId } } });

        foreach (var response in new[] { claim, self })
        {
            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("detail").GetString()
                .ShouldBe("You already have a pending request to link a reviewer profile.");
        }
    }

    [Fact]
    public async Task Claim_WithoutProof_Returns400()
    {
        var reviewerId = await CreateReviewerAsync(TestData.Unique("NoProof"), await CreateGenreAsync());
        var (_, user) = await NewUserAsync();

        using var response = await user.PostAsJsonAsync("api/claims", new { reviewerId, note = "   " });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("detail").GetString().ShouldBe("Proof must not be empty.");
    }

    #endregion

    #region Helpers

    private async Task<(string Id, HttpClient Client)> NewUserAsync()
    {
        var client = api.Factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@test.local";
        using var register = await client.PostAsJsonAsync("api/auth/register", new { name = "requester", email, password = ApiFixture.Password });
        register.EnsureSuccessStatusCode();
        var id = (await register.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await ApiFixture.LoginAsync(client, email));

        return (id, client);
    }

    private async Task<int> CreateGenreAsync()
    {
        using var admin = api.CreateClient(ApiFixture.Admin);
        using var response = await admin.PostAsJsonAsync("api/genres", new { title = TestData.Unique("Genre") });
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    private async Task<string> CreateReviewerAsync(string name, int genreId)
    {
        using var admin = api.CreateClient(ApiFixture.Admin);
        using var response = await admin.PostAsJsonAsync("api/reviewers", new { fullName = name, genres = new[] { new { id = genreId } } });
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;
    }

    private static async Task<string> SuggestAsync(HttpClient user, string name, int genreId)
    {
        using var response = await user.PostAsJsonAsync("api/suggestions", new { fullName = name, genres = new[] { new { id = genreId } } });
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;
    }

    private static async Task<string> ClaimAsync(HttpClient user, string reviewerId)
    {
        using var response = await user.PostAsJsonAsync("api/claims", new { reviewerId, note = "https://example.com/post-with-code" });
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;
    }

    #endregion
}
