using System.ComponentModel.DataAnnotations;
using ReReviewer.Data.CRUD;
using ReReviewer.Data.Entities.Enums;
using ReReviewer.Data.Models;
using ReReviewer.IntegrationTests.Infrastructure;

namespace ReReviewer.IntegrationTests.Data;

// Все критерии обязательны в каждом отзыве и всегда видны в рейтинге. Своя база: см. CriteriaDatabaseCollection.
[Collection(CriteriaDatabaseCollection.Name)]
public class RequiredCriteriaTests(DatabaseFixture database)
{
    [Fact]
    public async Task CreateAsync_WithoutEveryCriterion_ThrowsValidation()
    {
        await using var db = database.CreateContext();
        var criterion = await TestData.CriterionAsync(db);
        var user = await TestData.UserAsync(db);
        var reviewer = await TestData.ReviewerAsync(db);
        var scores = await TestData.AllCriteriaAsync(db, 80);
        scores.Remove(criterion.Id);

        var exception = await Should.ThrowAsync<ValidationException>(
            () => new ReviewService(db).CreateAsync(user.Id, reviewer.Id, new ReviewInput("Text", 8, [], scores)));

        exception.Message.ShouldBe("Every criterion must be scored.");
    }

    [Fact]
    public async Task CreateAsync_AcceptsZeroAndHundredPercent()
    {
        await using var db = database.CreateContext();
        var zero = await TestData.CriterionAsync(db);
        var hundred = await TestData.CriterionAsync(db);
        var user = await TestData.UserAsync(db);
        var reviewer = await TestData.ReviewerAsync(db);
        var scores = await TestData.AllCriteriaAsync(db, scores: new() { [zero.Id] = 0, [hundred.Id] = 100 });

        var review = await new ReviewService(db).CreateAsync(user.Id, reviewer.Id, new ReviewInput("Text", 8, [], scores));

        review.CriterionScores.ToDictionary(s => s.CriterionId, s => s.Score).ShouldBe(scores, ignoreOrder: true);
    }

    [Fact]
    public async Task CreateAsync_CriterionScoreOver100_ThrowsValidation()
    {
        await using var db = database.CreateContext();
        await TestData.CriterionAsync(db);
        var user = await TestData.UserAsync(db);
        var reviewer = await TestData.ReviewerAsync(db);

        var exception = await Should.ThrowAsync<ValidationException>(
            async () => await new ReviewService(db).CreateAsync(user.Id, reviewer.Id, new ReviewInput("Text", 8, [], await TestData.AllCriteriaAsync(db, 101))));

        exception.Message.ShouldBe("Criterion score must be between 0 and 100.");
    }

    // Средний процент — по опубликованным отзывам; у обзорщика без отзывов критерий всё равно есть, без оценок.
    [Fact]
    public async Task GetReviewerRatingAsync_ListsEveryCriterion_EvenWithoutScores()
    {
        await using var db = database.CreateContext();
        var criterion = await TestData.CriterionAsync(db);
        var reviewer = await TestData.ReviewerAsync(db);
        var unreviewed = await TestData.ReviewerAsync(db);
        var service = new ReviewService(db);

        foreach (var percent in new byte[] { 70, 40 })
        {
            var user = await TestData.UserAsync(db);
            var review = await service.CreateAsync(user.Id, reviewer.Id,
                new ReviewInput("Text", 7, [], await TestData.AllCriteriaAsync(db, scores: new() { [criterion.Id] = percent })));
            await service.SetStatusAsync(review.Id, ReviewStatus.Published);
        }

        await using var check = database.CreateContext();
        var rating = await new ReviewService(check).GetReviewerRatingAsync(reviewer.Id);
        var empty = await new ReviewService(check).GetReviewerRatingAsync(unreviewed.Id);

        var scored = rating.Criteria.Single(c => c.CriterionId == criterion.Id);
        scored.AverageScore.ShouldBe(55);
        scored.ScoreCount.ShouldBe(2);
        empty.Criteria.Count.ShouldBe(rating.Criteria.Count);
        var unscored = empty.Criteria.Single(c => c.CriterionId == criterion.Id);
        unscored.AverageScore.ShouldBeNull();
        unscored.ScoreCount.ShouldBe(0);
    }
}
