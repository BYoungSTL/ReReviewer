using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using ReReviewer.Data.CRUD;
using ReReviewer.Data.Entities.Enums;
using ReReviewer.IntegrationTests.Infrastructure;

namespace ReReviewer.IntegrationTests.Data;

[Collection(DatabaseCollection.Name)]
public class MaterialSuggestionServiceTests(DatabaseFixture database)
{
    [Fact]
    public async Task ApproveAsync_CreatesMaterialOfReviewer_AndClosesSuggestion()
    {
        var (user, reviewer, game) = await ArrangeAsync();
        var url = $"https://example.com/{Guid.NewGuid():N}";
        Guid suggestionId;
        await using (var db = database.CreateContext())
            suggestionId = (await new MaterialSuggestionService(db).CreateAsync(user, reviewer, game, "Обзор", url, MaterialFormat.Video, new DateOnly(2026, 11, 1))).Id;

        await using (var db = database.CreateContext())
            await new MaterialSuggestionService(db).ApproveAsync(suggestionId);

        await using var check = database.CreateContext();
        var suggestion = await check.MaterialSuggestions.SingleAsync(s => s.Id == suggestionId);
        suggestion.Status.ShouldBe(RequestStatus.Approved);
        suggestion.DecidedAt.ShouldNotBeNull();
        var material = await check.Materials.SingleAsync(m => m.Id == suggestion.MaterialId);
        (material.ReviewerId, material.GameId, material.Title, material.Url, material.Format, material.PublishedAt)
            .ShouldBe((reviewer, game, "Обзор", url, MaterialFormat.Video, (DateOnly?)new DateOnly(2026, 11, 1)));
    }

    [Fact]
    public async Task CreateAsync_LinkOfExistingMaterial_ThrowsValidation()
    {
        var (user, reviewer, game) = await ArrangeAsync();
        await using var db = database.CreateContext();
        var existing = await TestData.MaterialAsync(db, reviewer, game);

        var exception = await Should.ThrowAsync<ValidationException>(
            () => new MaterialSuggestionService(db).CreateAsync(user, reviewer, game, "Дубль", existing.Url, MaterialFormat.Article, null));

        exception.Message.ShouldBe("A material with this link already exists.");
    }

    [Fact]
    public async Task CreateAsync_LinkAlreadySuggested_ThrowsValidation()
    {
        var (user, reviewer, game) = await ArrangeAsync();
        var url = $"https://example.com/{Guid.NewGuid():N}";
        await using var db = database.CreateContext();
        var service = new MaterialSuggestionService(db);
        await service.CreateAsync(user, reviewer, game, "Первый", url, MaterialFormat.Video, null);

        var exception = await Should.ThrowAsync<ValidationException>(
            () => service.CreateAsync(user, reviewer, game, "Второй", url, MaterialFormat.Video, null));

        exception.Message.ShouldBe("This material has already been suggested and is waiting for moderation.");
    }

    [Fact]
    public async Task RejectAsync_ClosesWithoutMaterial_AndCannotBeDecidedTwice()
    {
        var (user, reviewer, game) = await ArrangeAsync();
        var url = $"https://example.com/{Guid.NewGuid():N}";
        await using var db = database.CreateContext();
        var service = new MaterialSuggestionService(db);
        var suggestion = await service.CreateAsync(user, reviewer, game, "Отклонят", url, MaterialFormat.Podcast, null);

        await service.RejectAsync(suggestion.Id);

        suggestion.Status.ShouldBe(RequestStatus.Rejected);
        (await db.Materials.AnyAsync(m => m.Url == url)).ShouldBeFalse();
        var exception = await Should.ThrowAsync<ValidationException>(() => service.ApproveAsync(suggestion.Id));
        exception.Message.ShouldBe("This request has already been decided.");
    }

    private async Task<(Guid User, Guid Reviewer, Guid Game)> ArrangeAsync()
    {
        await using var db = database.CreateContext();

        return ((await TestData.UserAsync(db)).Id, (await TestData.ReviewerAsync(db)).Id, (await TestData.GameAsync(db)).Id);
    }
}
