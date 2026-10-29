using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using ReReviewer.Data.CRUD;
using ReReviewer.Data.Entities.Enums;
using ReReviewer.Data.Models;
using ReReviewer.IntegrationTests.Infrastructure;

namespace ReReviewer.IntegrationTests.Data;

[Collection(DatabaseCollection.Name)]
public class ReviewServiceTests(DatabaseFixture database)
{
    private sealed record Setup(Guid UserId, Guid ReviewerId, Guid Material1, Guid Material2, int Criterion1, int Criterion2);

    #region CreateAsync

    [Fact]
    public async Task CreateAsync_SavesPendingReviewWithMaterialsAndScores()
    {
        var s = await ArrangeAsync();

        var reviewId = await CreateAsync(s, [s.Material1, s.Material2], new() { [s.Criterion1] = 7, [s.Criterion2] = 9 });

        await using var db = database.CreateContext();
        var review = await db.Reviews.SingleAsync(r => r.Id == reviewId);
        review.Status.ShouldBe(ReviewStatus.Pending);
        review.CreatedAt.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(-5));
        review.Materials.Select(m => m.Id).ShouldBe([s.Material1, s.Material2], ignoreOrder: true);
        review.CriterionScores.ToDictionary(c => c.CriterionId, c => c.Score).ShouldBe(new Dictionary<int, byte> { [s.Criterion1] = 7, [s.Criterion2] = 9 });
    }

    [Fact]
    public async Task CreateAsync_SecondReviewOfSameReviewer_ThrowsValidation()
    {
        var s = await ArrangeAsync();
        await CreateAsync(s, [], new());

        var exception = await Should.ThrowAsync<ValidationException>(() => CreateAsync(s, [], new()));

        exception.Message.ShouldBe("You have already reviewed this reviewer. Edit your existing review instead.");
    }

    [Fact]
    public async Task CreateAsync_MaterialOfAnotherReviewer_ThrowsValidation()
    {
        var s = await ArrangeAsync();
        var other = await ArrangeAsync();

        var exception = await Should.ThrowAsync<ValidationException>(() => CreateAsync(s, [other.Material1], new()));

        exception.Message.ShouldBe("A review can only reference materials of the reviewer it is about.");
    }

    [Fact]
    public async Task CreateAsync_UnknownCriterion_ThrowsKeyNotFound()
    {
        var s = await ArrangeAsync();

        await Should.ThrowAsync<KeyNotFoundException>(() => CreateAsync(s, [], new() { [int.MaxValue] = 5 }));
    }

    [Fact]
    public async Task CreateAsync_ScoreOutOfRange_ThrowsValidation()
    {
        var s = await ArrangeAsync();
        await using var db = database.CreateContext();

        await Should.ThrowAsync<ValidationException>(
            () => new ReviewService(db).CreateAsync(s.UserId, s.ReviewerId, new ReviewInput("Text", 11, [], new Dictionary<int, byte>())));
    }

    #endregion

    #region EditAsync / DeleteOwnAsync

    [Fact]
    public async Task EditAsync_SyncsMaterialsAndScores_AndReturnsToModeration()
    {
        var s = await ArrangeAsync();
        var reviewId = await CreateAsync(s, [s.Material1, s.Material2], new() { [s.Criterion1] = 7, [s.Criterion2] = 9 });
        await using (var db = database.CreateContext())
            await new ReviewService(db).SetStatusAsync(reviewId, ReviewStatus.Published);

        await using (var db = database.CreateContext())
            await new ReviewService(db).EditAsync(reviewId, s.UserId, new ReviewInput("Changed", 4, [s.Material2], new Dictionary<int, byte> { [s.Criterion1] = 3 }));

        await using var check = database.CreateContext();
        var review = await check.Reviews.SingleAsync(r => r.Id == reviewId);
        review.Text.ShouldBe("Changed");
        review.Score.ShouldBe((byte)4);
        review.Status.ShouldBe(ReviewStatus.Pending);
        review.UpdatedAt.ShouldNotBeNull();
        review.Materials.Select(m => m.Id).ShouldBe([s.Material2]);
        review.CriterionScores.Select(c => (c.CriterionId, c.Score)).ShouldBe([(s.Criterion1, (byte)3)]);
    }

    [Fact]
    public async Task EditAsync_ByAnotherUser_ThrowsUnauthorized()
    {
        var s = await ArrangeAsync();
        var reviewId = await CreateAsync(s, [], new());
        await using var db = database.CreateContext();

        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => new ReviewService(db).EditAsync(reviewId, Guid.NewGuid(), new ReviewInput("Hack", 1, [], new Dictionary<int, byte>())));
    }

    [Fact]
    public async Task DeleteOwnAsync_RemovesReviewWithScoresAndMaterialLinks()
    {
        var s = await ArrangeAsync();
        var reviewId = await CreateAsync(s, [s.Material1], new() { [s.Criterion1] = 7 });

        await using (var db = database.CreateContext())
            await new ReviewService(db).DeleteOwnAsync(reviewId, s.UserId);

        await using var check = database.CreateContext();
        (await check.Reviews.AnyAsync(r => r.Id == reviewId)).ShouldBeFalse();
        (await check.ReviewCriterionScores.AnyAsync(c => c.ReviewId == reviewId)).ShouldBeFalse();
        (await check.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM ReviewMaterials WHERE ReviewsId = {reviewId}").SingleAsync()).ShouldBe(0);
        (await check.Materials.AnyAsync(m => m.Id == s.Material1)).ShouldBeTrue();
    }

    #endregion

    [Fact]
    public async Task GetReviewerRatingAsync_CountsOnlyPublished()
    {
        var s = await ArrangeAsync();
        await using var db = database.CreateContext();
        var anotherUser = await TestData.UserAsync(db);
        var service = new ReviewService(db);
        var published = await service.CreateAsync(s.UserId, s.ReviewerId, new ReviewInput("Good", 6, [], new Dictionary<int, byte> { [s.Criterion1] = 8 }));
        await service.CreateAsync(anotherUser.Id, s.ReviewerId, new ReviewInput("Pending", 10, [], new Dictionary<int, byte> { [s.Criterion1] = 1 }));
        await service.SetStatusAsync(published.Id, ReviewStatus.Published);

        await using var check = database.CreateContext();
        var rating = await new ReviewService(check).GetReviewerRatingAsync(s.ReviewerId);

        rating.ReviewCount.ShouldBe(1);
        rating.AverageScore.ShouldBe(6);
        rating.Criteria.Single().AverageScore.ShouldBe(8);
    }

    private async Task<Setup> ArrangeAsync()
    {
        await using var db = database.CreateContext();
        var user = await TestData.UserAsync(db);
        var reviewer = await TestData.ReviewerAsync(db);
        var game = await TestData.GameAsync(db);
        var material1 = await TestData.MaterialAsync(db, reviewer.Id, game.Id);
        var material2 = await TestData.MaterialAsync(db, reviewer.Id, game.Id);
        var criterion1 = await TestData.CriterionAsync(db);
        var criterion2 = await TestData.CriterionAsync(db);

        return new Setup(user.Id, reviewer.Id, material1.Id, material2.Id, criterion1.Id, criterion2.Id);
    }

    private async Task<Guid> CreateAsync(Setup s, Guid[] materials, Dictionary<int, byte> scores)
    {
        await using var db = database.CreateContext();
        var review = await new ReviewService(db).CreateAsync(s.UserId, s.ReviewerId, new ReviewInput("Text", 8, materials, scores));

        return review.Id;
    }
}