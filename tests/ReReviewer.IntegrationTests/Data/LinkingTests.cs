using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using ReReviewer.Data.CRUD;
using ReReviewer.Data.Entities;
using ReReviewer.Data.Entities.Enums;
using ReReviewer.IntegrationTests.Infrastructure;

namespace ReReviewer.IntegrationTests.Data;

[Collection(DatabaseCollection.Name)]
public class LinkingTests(DatabaseFixture database)
{
    #region BaseService.GetByIdsAsync

    [Fact]
    public async Task GetByIdsAsync_AllExist_ReturnsTrackedEntities()
    {
        int first, second;
        await using (var db = database.CreateContext())
            (first, second) = ((await TestData.GenreAsync(db)).Id, (await TestData.GenreAsync(db)).Id);

        await using var context = database.CreateContext();
        var genres = await new GenreService(context).GetByIdsAsync([first, second, first]);

        genres.Select(g => g.Id).ShouldBe([first, second], ignoreOrder: true);
        genres.ShouldAllBe(g => context.Entry(g).State == EntityState.Unchanged);
    }

    [Fact]
    public async Task GetByIdsAsync_MissingId_ThrowsKeyNotFound()
    {
        int existing;
        await using (var db = database.CreateContext())
            existing = (await TestData.GenreAsync(db)).Id;

        await using var context = database.CreateContext();
        await Should.ThrowAsync<KeyNotFoundException>(() => new GenreService(context).GetByIdsAsync([existing, int.MaxValue]));
    }

    #endregion

    #region OutletService.LinkReviewersAsync

    [Fact]
    public async Task LinkReviewersAsync_CalledTwice_DoesNotDuplicateLinks()
    {
        Guid outletId, reviewerId;
        await using (var db = database.CreateContext())
            (outletId, reviewerId) = ((await TestData.OutletAsync(db)).Id, (await TestData.ReviewerAsync(db)).Id);

        for (var i = 0; i < 2; i++)
        {
            await using var db = database.CreateContext();
            await new OutletService(db).LinkReviewersAsync(outletId, [new Reviewer { Id = reviewerId }]);
        }

        await using var check = database.CreateContext();
        (await check.ReviewerOutlets.CountAsync(l => l.OutletId == outletId)).ShouldBe(1);
    }

    #endregion

    #region ReviewerService

    [Fact]
    public async Task LinkMaterialsAsync_CreatesNewAndSkipsOwnExisting()
    {
        Guid reviewerId, gameId, ownMaterialId;
        await using (var db = database.CreateContext())
        {
            reviewerId = (await TestData.ReviewerAsync(db)).Id;
            gameId = (await TestData.GameAsync(db)).Id;
            ownMaterialId = (await TestData.MaterialAsync(db, reviewerId, gameId)).Id;
        }

        await using (var db = database.CreateContext())
            await new ReviewerService(db).LinkMaterialsAsync(new Reviewer { Id = reviewerId },
            [
                new Material { Id = ownMaterialId },
                new Material { GameId = gameId, Title = "New", Url = $"https://example.com/{Guid.NewGuid():N}", Format = MaterialFormat.Article }
            ]);

        await using var check = database.CreateContext();
        (await check.Materials.CountAsync(m => m.ReviewerId == reviewerId)).ShouldBe(2);
    }

    [Fact]
    public async Task LinkMaterialsAsync_ForeignMaterial_ThrowsAndAddsNothing()
    {
        Guid reviewerId, gameId, foreignMaterialId;
        await using (var db = database.CreateContext())
        {
            reviewerId = (await TestData.ReviewerAsync(db)).Id;
            var other = await TestData.ReviewerAsync(db);
            gameId = (await TestData.GameAsync(db)).Id;
            foreignMaterialId = (await TestData.MaterialAsync(db, other.Id, gameId)).Id;
        }

        await using (var db = database.CreateContext())
            await Should.ThrowAsync<ValidationException>(() => new ReviewerService(db).LinkMaterialsAsync(new Reviewer { Id = reviewerId },
            [
                new Material { GameId = gameId, Title = "New", Url = $"https://example.com/{Guid.NewGuid():N}", Format = MaterialFormat.Video },
                new Material { Id = foreignMaterialId }
            ]));

        await using var check = database.CreateContext();
        (await check.Materials.AnyAsync(m => m.ReviewerId == reviewerId)).ShouldBeFalse();
    }

    [Fact]
    public async Task ValidateReviewerAsync_LinksProfile_AndRejectsSecondAccount()
    {
        Guid reviewerId, firstUser, secondUser;
        await using (var db = database.CreateContext())
        {
            reviewerId = (await TestData.ReviewerAsync(db)).Id;
            (firstUser, secondUser) = ((await TestData.UserAsync(db)).Id, (await TestData.UserAsync(db)).Id);
        }

        await using (var db = database.CreateContext())
            (await new ReviewerService(db).ValidateReviewerAsync(new Reviewer { Id = reviewerId }, firstUser)).UserId.ShouldBe(firstUser);

        await using var context = database.CreateContext();
        var exception = await Should.ThrowAsync<ValidationException>(
            () => new ReviewerService(context).ValidateReviewerAsync(new Reviewer { Id = reviewerId }, secondUser));
        exception.Message.ShouldBe("This reviewer profile is already linked to another user.");
    }

    #endregion
}