using System.Net.Http.Json;
using System.Text.Json;
using ReReviewer.IntegrationTests.Infrastructure;

namespace ReReviewer.IntegrationTests.Api;

// Документ доступен только в Development, поэтому здесь отдельное приложение в этом окружении.
// Заодно проверяет AuthorizationOperationTransformer на настоящих атрибутах контроллеров.
[Collection(ApiCollection.Name)]
public class OpenApiDocumentTests(ApiFixture api)
{
    [Fact]
    public async Task Document_MarksOnlyProtectedEndpoints_AndListsRequiredRoles()
    {
        await using var factory = new ApiFactory(api.ConnectionString, "Development");
        using var client = factory.CreateClient();

        var document = await client.GetFromJsonAsync<JsonElement>("openapi/v1.json");
        var paths = document.GetProperty("paths");
        JsonElement Operation(string path, string method) => paths.GetProperty(path).GetProperty(method);
        bool Secured(string path, string method) => Operation(path, method).TryGetProperty("security", out _);
        string Description(string path, string method) =>
            Operation(path, method).TryGetProperty("description", out var d) ? d.GetString() ?? "" : "";

        document.GetProperty("openapi").GetString()!.ShouldStartWith("3.1");
        document.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer").GetProperty("scheme").GetString().ShouldBe("bearer");

        Secured("/api/auth/login", "post").ShouldBeFalse();
        Secured("/api/genres", "get").ShouldBeFalse();
        Secured("/api/reviews/{id}", "get").ShouldBeFalse();

        Secured("/api/auth/me", "get").ShouldBeTrue();
        Secured("/api/reviews", "post").ShouldBeTrue();
        Secured("/api/genres", "post").ShouldBeTrue();

        Description("/api/users", "get").ShouldContain("Requires role: Admin.");
        Description("/api/genres", "post").ShouldContain("Requires role: Admin or Moderator.");
        Description("/api/users/me", "put").ShouldNotContain("Requires role");
    }

    [Fact]
    public async Task Document_IsNotServedOutsideDevelopment()
    {
        using var client = api.Factory.CreateClient();

        (await client.GetAsync("openapi/v1.json")).IsSuccessStatusCode.ShouldBeFalse();
    }
}