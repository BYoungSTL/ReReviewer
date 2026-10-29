using Microsoft.EntityFrameworkCore;
using ReReviewer.Data.CRUD;
using ReReviewer.Data.Models;
using ReReviewer.IntegrationTests.Infrastructure;

namespace ReReviewer.IntegrationTests.Data;

// Каскады и запреты удаления из конфигурации ReReviewerDbContext — их проверяет только настоящий SQL Server.
[Collection(DatabaseCollection.Name)]
public class DeleteRulesTests(DatabaseFixture database)
{
    [Fact]
    public async Task Reviewer_WithReviews_CannotBeDeleted()
    {
        Guid reviewerId;
        await using (var db = database.CreateContext())
        {
            var user = await TestData.UserAsync(db);
            reviewerId = (await TestData.ReviewerAsync(db)).Id;
            await new ReviewService(db).CreateAsync(user.Id, reviewerId, new ReviewInput("Text", 8, [], new Dictionary<int, byte>()));
        }

        await using var context = database.CreateContext();
        await Should.ThrowAsync<DbUpdateException>(() => new ReviewerService(context).DeleteAsync(reviewerId));
    }

    [Fact]
    public async Task Game_WithMaterials_CannotBeDeleted()
    {
        Guid gameId;
        await using (var db = database.CreateContext())
        {
            var reviewer = await TestData.ReviewerAsync(db);
            gameId = (await TestData.GameAsync(db)).Id;
            await TestData.MaterialAsync(db, reviewer.Id, gameId);
        }

        await using var context = database.CreateContext();
        await Should.ThrowAsync<DbUpdateException>(() => new GameService(context).DeleteAsync(gameId));
    }

    [Fact]
    public async Task DeletingMaterial_KeepsReview_AndRemovesLink()
    {
        Guid reviewId, materialId;
        await using (var db = database.CreateContext())
        {
            var user = await TestData.UserAsync(db);
            var reviewer = await TestData.ReviewerAsync(db);
            var game = await TestData.GameAsync(db);
            materialId = (await TestData.MaterialAsync(db, reviewer.Id, game.Id)).Id;
            reviewId = (await new ReviewService(db).CreateAsync(user.Id, reviewer.Id, new ReviewInput("Text", 8, [materialId], new Dictionary<int, byte>()))).Id;
        }

        await using (var db = database.CreateContext())
            (await new MaterialService(db).DeleteAsync(materialId)).ShouldBeTrue();

        await using var check = database.CreateContext();
        var review = await check.Reviews.SingleAsync(r => r.Id == reviewId);
        review.Materials.ShouldBeEmpty();
    }

    [Fact]
    public async Task DeletingUser_RemovesTheirReviews_AndUnlinksReviewerProfile()
    {
        Guid userId, reviewId, profileId;
        await using (var db = database.CreateContext())
        {
            var user = await TestData.UserAsync(db);
            userId = user.Id;
            profileId = (await TestData.ReviewerAsync(db, userId)).Id;
            var reviewed = await TestData.ReviewerAsync(db);
            reviewId = (await new ReviewService(db).CreateAsync(userId, reviewed.Id, new ReviewInput("Text", 8, [], new Dictionary<int, byte>()))).Id;
        }

        await using (var db = database.CreateContext())
            (await new UserService(db).DeleteAsync(userId)).ShouldBeTrue();

        await using var check = database.CreateContext();
        (await check.Reviews.AnyAsync(r => r.Id == reviewId)).ShouldBeFalse();
        (await check.Reviewers.SingleAsync(r => r.Id == profileId)).UserId.ShouldBeNull();
    }
}