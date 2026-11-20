using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ReReviewer.IntegrationTests.Infrastructure;

namespace ReReviewer.IntegrationTests.Api;

// Номинация обзорщика на знак: читатели номинируют, модератор видит число номинаций и выдаёт знак.
[Collection(ApiCollection.Name)]
public class NominationsApiTests(ApiFixture api)
{
    [Fact]
    public async Task Nominations_AreCountedForModerators_AndApprovalAwardsBadge()
    {
        var reviewerId = await ReviewerAsync();
        var badgeId = int.Parse(await CreateAsync("api/badges", new { title = TestData.Unique("Badge") }));
        var first = await ReaderAsync();
        var second = await ReaderAsync();

        foreach (var reader in new[] { first, second })
            (await reader.PostAsJsonAsync("api/nominations", new { reviewerId, badgeId })).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await first.GetFromJsonAsync<int[]>($"api/nominations/my?reviewerId={reviewerId}")).ShouldBe([badgeId]);

        using var moderator = api.CreateClient(ApiFixture.Moderator);
        var queue = await moderator.GetFromJsonAsync<JsonElement>("api/nominations/moderation?pageSize=100");
        var row = queue.GetProperty("items").EnumerateArray()
            .Single(r => r.GetProperty("reviewerId").GetString() == reviewerId && r.GetProperty("badgeId").GetInt32() == badgeId);
        row.GetProperty("nominationCount").GetInt32().ShouldBe(2);
        row.GetProperty("firstNominatedAt").GetString().ShouldEndWith("Z");

        (await moderator.PostAsync($"api/nominations/{reviewerId}/{badgeId}/approve", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var reviewer = await moderator.GetFromJsonAsync<JsonElement>($"api/reviewers/{reviewerId}");
        reviewer.GetProperty("badges").EnumerateArray().Select(b => b.GetProperty("badgeId").GetInt32()).ShouldContain(badgeId);
        using var again = await (await ReaderAsync()).PostAsJsonAsync("api/nominations", new { reviewerId, badgeId });
        again.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await again.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("detail").GetString().ShouldBe("This reviewer already has this badge.");
    }

    private async Task<HttpClient> ReaderAsync()
    {
        var client = api.Factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@test.local";
        (await client.PostAsJsonAsync("api/auth/register", new { name = "reader", email, password = ApiFixture.Password })).EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await ApiFixture.LoginAsync(client, email));

        return client;
    }

    private async Task<string> ReviewerAsync()
    {
        var genre = await CreateAsync("api/genres", new { title = TestData.Unique("Genre") });

        return await CreateAsync("api/reviewers", new { fullName = TestData.Unique("Reviewer"), genres = new[] { new { id = int.Parse(genre) } } });
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
