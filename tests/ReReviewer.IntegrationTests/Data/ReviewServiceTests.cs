using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using ReReviewer.Data.CRUD;
using ReReviewer.Data.Entities.Enums;
using ReReviewer.Data.Models;
using ReReviewer.IntegrationTests.Infrastructure;

namespace ReReviewer.IntegrationTests.Data;

// Тесты заводят критерии, а каждый критерий обязателен во всех отзывах базы, поэтому они — в отдельной базе,
// и каждый отзыв здесь оценивает все её критерии (TestData.AllCriteriaAsync).
[Collection(CriteriaDatabaseCollection.Name)]
public class ReviewServiceTests(DatabaseFixture database)
{
    private sealed record Setup(Guid UserId, Guid ReviewerId, Guid GameId, Guid Material1, Guid Material2, int Criterion1, int Criterion2);

    #region CreateAsync

    [Fact]
    public async Task CreateAsync_SavesPendingReviewWithMaterialsAndScores()
    {
        var s = await ArrangeAsync();

        var reviewId = await CreateAsync(s, [s.Material1, s.Material2], new() { [s.Criterion1] = 70, [s.Criterion2] = 90 });

        await using var db = database.CreateContext();
        var review = await db.Reviews.SingleAsync(r => r.Id == reviewId);
        review.Status.ShouldBe(ReviewStatus.Pending);
        review.CreatedAt.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(-5));
        review.Materials.Select(m => m.Id).ShouldBe([s.Material1, s.Material2], ignoreOrder: true);
        review.CriterionScores.Count.ShouldBe(await db.Criteria.CountAsync());
        Scores(review, s).ShouldBe([(s.Criterion1, (byte)70), (s.Criterion2, (byte)90)]);
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
    public async Task CreateAsync_OwnReviewerProfile_ThrowsValidation()
    {
        await using var db = database.CreateContext();
        var user = await TestData.UserAsync(db);
        var reviewer = await TestData.ReviewerAsync(db, user.Id);

        var exception = await Should.ThrowAsync<ValidationException>(
            async () => await new ReviewService(db).CreateAsync(user.Id, reviewer.Id, new ReviewInput("I am the best", 10, [], await TestData.AllCriteriaAsync(db))));

        exception.Message.ShouldBe("You cannot review your own reviewer profile.");
    }

    [Fact]
    public async Task EditAsync_AfterProfileLinkedToAuthor_ThrowsValidation()
    {
        var s = await ArrangeAsync();
        var reviewId = await CreateAsync(s, [], new());
        await using (var db = database.CreateContext())
            await db.Reviewers.Where(r => r.Id == s.ReviewerId).ExecuteUpdateAsync(r => r.SetProperty(x => x.UserId, s.UserId));

        await using var edit = database.CreateContext();
        var exception = await Should.ThrowAsync<ValidationException>(
            async () => await new ReviewService(edit).EditAsync(reviewId, s.UserId, new ReviewInput("Now about myself", 10, [], await TestData.AllCriteriaAsync(edit))));

        exception.Message.ShouldBe("You cannot review your own reviewer profile.");
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

        await Should.ThrowAsync<KeyNotFoundException>(() => CreateAsync(s, [], new() { [int.MaxValue] = 50 }));
    }

    [Fact]
    public async Task CreateAsync_ScoreOutOfRange_ThrowsValidation()
    {
        var s = await ArrangeAsync();
        await using var db = database.CreateContext();

        await Should.ThrowAsync<ValidationException>(
            async () => await new ReviewService(db).CreateAsync(s.UserId, s.ReviewerId, new ReviewInput("Text", 11, [], await TestData.AllCriteriaAsync(db))));
    }

    #endregion

    #region EditAsync / DeleteOwnAsync

    [Fact]
    public async Task EditAsync_SyncsMaterialsAndScores_AndReturnsToModeration()
    {
        var s = await ArrangeAsync();
        var reviewId = await CreateAsync(s, [s.Material1, s.Material2], new() { [s.Criterion1] = 70, [s.Criterion2] = 90 });
        await using (var db = database.CreateContext())
            await new ReviewService(db).SetStatusAsync(reviewId, ReviewStatus.Published);

        await using (var db = database.CreateContext())
            await new ReviewService(db).EditAsync(reviewId, s.UserId, new ReviewInput("Changed", 4, [s.Material2],
                await TestData.AllCriteriaAsync(db, scores: new() { [s.Criterion1] = 30, [s.Criterion2] = 55 })));

        await using var check = database.CreateContext();
        var review = await check.Reviews.SingleAsync(r => r.Id == reviewId);
        review.Text.ShouldBe("Changed");
        review.Score.ShouldBe((byte)4);
        review.Status.ShouldBe(ReviewStatus.Pending);
        review.UpdatedAt.ShouldNotBeNull();
        review.Materials.Select(m => m.Id).ShouldBe([s.Material2]);
        Scores(review, s).ShouldBe([(s.Criterion1, (byte)30), (s.Criterion2, (byte)55)]);
    }

    // datetime2 не хранит часовой пояс: без UtcDateTimeConverter EF вернул бы даты с DateTimeKind.Unspecified.
    [Fact]
    public async Task Dates_AreReadAsUtc()
    {
        var s = await ArrangeAsync();
        var reviewId = await CreateAsync(s, [], new());
        await using (var db = database.CreateContext())
            await new ReviewService(db).EditAsync(reviewId, s.UserId, new ReviewInput("Changed", 4, [], await TestData.AllCriteriaAsync(db)));

        await using var check = database.CreateContext();
        var review = await check.Reviews.SingleAsync(r => r.Id == reviewId);

        review.CreatedAt.Kind.ShouldBe(DateTimeKind.Utc);
        review.UpdatedAt.ShouldNotBeNull().Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Fact]
    public async Task EditAsync_ByAnotherUser_ThrowsUnauthorized()
    {
        var s = await ArrangeAsync();
        var reviewId = await CreateAsync(s, [], new());
        await using var db = database.CreateContext();

        await Should.ThrowAsync<UnauthorizedAccessException>(
            async () => await new ReviewService(db).EditAsync(reviewId, Guid.NewGuid(), new ReviewInput("Hack", 1, [], await TestData.AllCriteriaAsync(db))));
    }

    [Fact]
    public async Task DeleteOwnAsync_RemovesReviewWithScoresAndMaterialLinks()
    {
        var s = await ArrangeAsync();
        var reviewId = await CreateAsync(s, [s.Material1], new());

        await using (var db = database.CreateContext())
            await new ReviewService(db).DeleteOwnAsync(reviewId, s.UserId);

        await using var check = database.CreateContext();
        (await check.Reviews.AnyAsync(r => r.Id == reviewId)).ShouldBeFalse();
        (await check.ReviewCriterionScores.AnyAsync(c => c.ReviewId == reviewId)).ShouldBeFalse();
        (await check.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM ReviewMaterials WHERE ReviewsId = {reviewId}").SingleAsync()).ShouldBe(0);
        (await check.Materials.AnyAsync(m => m.Id == s.Material1)).ShouldBeTrue();
    }

    #endregion

    #region Новые материалы: модерация вместе с отзывом

    [Fact]
    public async Task CreateAsync_WithNewMaterial_KeepsItAsPendingSuggestionOfReview()
    {
        var s = await ArrangeAsync();
        var material = NewMaterial(s);

        var reviewId = await CreateAsync(s, [s.Material1], new(), [material]);

        await using var check = database.CreateContext();
        var suggestion = await check.MaterialSuggestions.SingleAsync(x => x.ReviewId == reviewId);
        (suggestion.UserId, suggestion.ReviewerId, suggestion.GameId, suggestion.Title, suggestion.Url, suggestion.Format, suggestion.Status)
            .ShouldBe((s.UserId, s.ReviewerId, s.GameId, material.Title, material.Url, MaterialFormat.Article, RequestStatus.Pending));
        (await check.Materials.AnyAsync(m => m.Url == material.Url)).ShouldBeFalse();
        (await check.Reviews.SingleAsync(r => r.Id == reviewId)).Materials.Select(m => m.Id).ShouldBe([s.Material1]);
    }

    [Fact]
    public async Task SetStatusAsync_Published_CreatesNewMaterialsAndMarksThemInReview()
    {
        var s = await ArrangeAsync();
        var material = NewMaterial(s);
        var reviewId = await CreateAsync(s, [s.Material1], new(), [material]);

        await using (var db = database.CreateContext())
            await new ReviewService(db).SetStatusAsync(reviewId, ReviewStatus.Published);

        await using var check = database.CreateContext();
        var created = await check.Materials.SingleAsync(m => m.Url == material.Url);
        (created.ReviewerId, created.GameId, created.Title, created.Format, created.PublishedAt)
            .ShouldBe((s.ReviewerId, s.GameId, material.Title, MaterialFormat.Article, material.PublishedAt));
        var review = await check.Reviews.SingleAsync(r => r.Id == reviewId);
        review.Status.ShouldBe(ReviewStatus.Published);
        review.Materials.Select(m => m.Id).ShouldBe([s.Material1, created.Id], ignoreOrder: true);
        var suggestion = await check.MaterialSuggestions.SingleAsync(x => x.ReviewId == reviewId);
        suggestion.Status.ShouldBe(RequestStatus.Approved);
        suggestion.DecidedAt.ShouldNotBeNull();
        suggestion.MaterialId.ShouldBe(created.Id);
    }

    // Пока отзыв ждал, модератор мог сам добавить материал с этой ссылкой: отзыв просто отмечает его.
    [Fact]
    public async Task SetStatusAsync_Published_LinkAddedMeanwhile_MarksExistingMaterial()
    {
        var s = await ArrangeAsync();
        var material = NewMaterial(s);
        var reviewId = await CreateAsync(s, [], new(), [material]);
        await using (var db = database.CreateContext())
            await db.Materials.Where(m => m.Id == s.Material2).ExecuteUpdateAsync(m => m.SetProperty(x => x.Url, material.Url));

        await using (var db = database.CreateContext())
            await new ReviewService(db).SetStatusAsync(reviewId, ReviewStatus.Published);

        await using var check = database.CreateContext();
        (await check.Materials.CountAsync(m => m.Url == material.Url)).ShouldBe(1);
        (await check.Reviews.SingleAsync(r => r.Id == reviewId)).Materials.Select(m => m.Id).ShouldBe([s.Material2]);
        (await check.MaterialSuggestions.SingleAsync(x => x.ReviewId == reviewId)).MaterialId.ShouldBe(s.Material2);
    }

    [Fact]
    public async Task SetStatusAsync_Rejected_RejectsNewMaterialsWithoutCreatingThem()
    {
        var s = await ArrangeAsync();
        var material = NewMaterial(s);
        var reviewId = await CreateAsync(s, [], new(), [material]);

        await using (var db = database.CreateContext())
            await new ReviewService(db).SetStatusAsync(reviewId, ReviewStatus.Rejected);

        await using var check = database.CreateContext();
        var suggestion = await check.MaterialSuggestions.SingleAsync(x => x.ReviewId == reviewId);
        suggestion.Status.ShouldBe(RequestStatus.Rejected);
        suggestion.DecidedAt.ShouldNotBeNull();
        (await check.Materials.AnyAsync(m => m.Url == material.Url)).ShouldBeFalse();
    }

    // Отклонённый отзыв правят и отправляют снова: новые материалы задаются заново, свою же ссылку можно оставить.
    [Fact]
    public async Task EditAsync_ReplacesUndecidedNewMaterials()
    {
        var s = await ArrangeAsync();
        var kept = NewMaterial(s);
        var dropped = NewMaterial(s);
        var reviewId = await CreateAsync(s, [], new(), [kept, dropped]);
        await using (var db = database.CreateContext())
            await new ReviewService(db).SetStatusAsync(reviewId, ReviewStatus.Rejected);
        var added = NewMaterial(s);

        await using (var db = database.CreateContext())
            await new ReviewService(db).EditAsync(reviewId, s.UserId,
                new ReviewInput("Changed", 7, [], await TestData.AllCriteriaAsync(db), [kept, added]));

        await using var check = database.CreateContext();
        var suggestions = await check.MaterialSuggestions.Where(x => x.ReviewId == reviewId).ToListAsync();
        suggestions.Select(x => x.Url).ShouldBe([kept.Url, added.Url], ignoreOrder: true);
        suggestions.ShouldAllBe(x => x.Status == RequestStatus.Pending);
        (await check.MaterialSuggestions.AnyAsync(x => x.Url == dropped.Url)).ShouldBeFalse();
    }

    // Одобренные вместе с прошлой версией отзыва заявки остаются: их материалы уже у обзорщика.
    [Fact]
    public async Task EditAsync_KeepsApprovedSuggestions()
    {
        var s = await ArrangeAsync();
        var material = NewMaterial(s);
        var reviewId = await CreateAsync(s, [], new(), [material]);
        await using (var db = database.CreateContext())
            await new ReviewService(db).SetStatusAsync(reviewId, ReviewStatus.Published);

        await using (var db = database.CreateContext())
            await new ReviewService(db).EditAsync(reviewId, s.UserId, new ReviewInput("Changed", 7, [], await TestData.AllCriteriaAsync(db)));

        await using var check = database.CreateContext();
        (await check.MaterialSuggestions.SingleAsync(x => x.ReviewId == reviewId)).Status.ShouldBe(RequestStatus.Approved);
        (await check.Materials.AnyAsync(m => m.Url == material.Url)).ShouldBeTrue();
    }

    [Fact]
    public async Task CreateAsync_NewMaterialWithLinkOfExistingMaterial_ThrowsValidation()
    {
        var s = await ArrangeAsync();
        await using var db = database.CreateContext();
        var existingUrl = (await db.Materials.SingleAsync(m => m.Id == s.Material1)).Url;

        var exception = await Should.ThrowAsync<ValidationException>(
            () => CreateAsync(s, [], new(), [NewMaterial(s) with { Url = existingUrl }]));

        exception.Message.ShouldBe("A material with this link already exists.");
    }

    [Fact]
    public async Task CreateAsync_NewMaterialAlreadySuggested_ThrowsValidation()
    {
        var s = await ArrangeAsync();
        var material = NewMaterial(s);
        await using (var db = database.CreateContext())
            await new MaterialSuggestionService(db).CreateAsync(s.UserId, s.ReviewerId, s.GameId, "Отдельно", material.Url, MaterialFormat.Video, null);

        var exception = await Should.ThrowAsync<ValidationException>(() => CreateAsync(s, [], new(), [material]));

        exception.Message.ShouldBe("This material has already been suggested and is waiting for moderation.");
    }

    [Fact]
    public async Task CreateAsync_SameLinkTwice_ThrowsValidation()
    {
        var s = await ArrangeAsync();
        var material = NewMaterial(s);

        var exception = await Should.ThrowAsync<ValidationException>(
            () => CreateAsync(s, [], new(), [material, material with { Title = "Again", Url = material.Url.ToUpperInvariant() }]));

        exception.Message.ShouldBe("Each new material must have its own link.");
    }

    [Fact]
    public async Task CreateAsync_TooManyNewMaterials_ThrowsValidation()
    {
        var s = await ArrangeAsync();

        var exception = await Should.ThrowAsync<ValidationException>(
            () => CreateAsync(s, [], new(), Enumerable.Range(0, 6).Select(_ => NewMaterial(s)).ToArray()));

        exception.Message.ShouldBe("A review can add at most 5 new materials.");
    }

    [Fact]
    public async Task CreateAsync_NewMaterialOfUnknownGame_ThrowsKeyNotFound()
    {
        var s = await ArrangeAsync();

        await Should.ThrowAsync<KeyNotFoundException>(() => CreateAsync(s, [], new(), [NewMaterial(s) with { GameId = Guid.NewGuid() }]));
    }

    // Заявки из отзыва модерируются только вместе с ним: в очереди материалов их нет, решить их отдельно нельзя.
    [Fact]
    public async Task SuggestionOfReview_IsNotInMaterialQueue_AndCannotBeDecidedSeparately()
    {
        var s = await ArrangeAsync();
        int pendingBefore;
        await using (var db = database.CreateContext())
            pendingBefore = await new MaterialSuggestionService(db).CountPendingAsync();
        var reviewId = await CreateAsync(s, [], new(), [NewMaterial(s)]);

        await using var check = database.CreateContext();
        var service = new MaterialSuggestionService(check);
        (await service.CountPendingAsync()).ShouldBe(pendingBefore);
        (await service.GetByUserAsync(s.UserId)).ShouldBeEmpty();
        var suggestion = await check.MaterialSuggestions.SingleAsync(x => x.ReviewId == reviewId);
        var exception = await Should.ThrowAsync<ValidationException>(() => service.ApproveAsync(suggestion.Id));
        exception.Message.ShouldBe("This material is part of a review and is moderated together with it.");
    }

    [Fact]
    public async Task DeleteOwnAsync_RemovesNewMaterialsOfReview()
    {
        var s = await ArrangeAsync();
        var reviewId = await CreateAsync(s, [], new(), [NewMaterial(s)]);

        await using (var db = database.CreateContext())
            await new ReviewService(db).DeleteOwnAsync(reviewId, s.UserId);

        await using var check = database.CreateContext();
        (await check.MaterialSuggestions.AnyAsync(x => x.ReviewId == reviewId)).ShouldBeFalse();
    }

    // У связи заявки с отзывом нет каскада в базе: удаление автора снимает отзыв и заявку одним запросом.
    [Fact]
    public async Task DeletingAuthor_RemovesReviewWithNewMaterials()
    {
        var s = await ArrangeAsync();
        var reviewId = await CreateAsync(s, [], new(), [NewMaterial(s)]);

        await using (var db = database.CreateContext())
            await db.Users.Where(u => u.Id == s.UserId).ExecuteDeleteAsync();

        await using var check = database.CreateContext();
        (await check.Reviews.AnyAsync(r => r.Id == reviewId)).ShouldBeFalse();
        (await check.MaterialSuggestions.AnyAsync(x => x.ReviewId == reviewId)).ShouldBeFalse();
    }

    #endregion

    [Fact]
    public async Task GetReviewerRatingAsync_CountsOnlyPublished()
    {
        var s = await ArrangeAsync();
        await using var db = database.CreateContext();
        var anotherUser = await TestData.UserAsync(db);
        var service = new ReviewService(db);
        var published = await service.CreateAsync(s.UserId, s.ReviewerId,
            new ReviewInput("Good", 6, [], await TestData.AllCriteriaAsync(db, scores: new() { [s.Criterion1] = 80 })));
        await service.CreateAsync(anotherUser.Id, s.ReviewerId,
            new ReviewInput("Pending", 10, [], await TestData.AllCriteriaAsync(db, scores: new() { [s.Criterion1] = 10 })));
        await service.SetStatusAsync(published.Id, ReviewStatus.Published);

        await using var check = database.CreateContext();
        var rating = await new ReviewService(check).GetReviewerRatingAsync(s.ReviewerId);

        rating.ReviewCount.ShouldBe(1);
        rating.AverageScore.ShouldBe(6);
        rating.Criteria.Single(c => c.CriterionId == s.Criterion1).AverageScore.ShouldBe(80);
        rating.Scores.Select(s => (int)s.Score).ShouldBe(Enumerable.Range(1, 10));
        rating.Scores.Where(s => s.Count > 0).ShouldBe([new ScoreCount(6, 1)]);
    }

    [Fact]
    public async Task GetPublishedSummariesAsync_AveragesOnlyPublished_AndFiltersByReviewer()
    {
        var s = await ArrangeAsync();
        await using var db = database.CreateContext();
        var second = await TestData.UserAsync(db);
        var third = await TestData.UserAsync(db);
        var unreviewed = await TestData.ReviewerAsync(db);
        var service = new ReviewService(db);
        var first = await service.CreateAsync(s.UserId, s.ReviewerId, new ReviewInput("Fine", 6, [], await TestData.AllCriteriaAsync(db)));
        var good = await service.CreateAsync(second.Id, s.ReviewerId, new ReviewInput("Good", 9, [], await TestData.AllCriteriaAsync(db)));
        await service.CreateAsync(third.Id, s.ReviewerId, new ReviewInput("Pending", 1, [], await TestData.AllCriteriaAsync(db)));
        await service.SetStatusAsync(first.Id, ReviewStatus.Published);
        await service.SetStatusAsync(good.Id, ReviewStatus.Published);

        await using var check = database.CreateContext();
        var summaries = await new ReviewService(check).GetPublishedSummariesAsync([s.ReviewerId, unreviewed.Id]);

        summaries.Keys.ShouldBe([s.ReviewerId]);
        summaries[s.ReviewerId].ShouldBe(new ReviewerScoreSummary(s.ReviewerId, 2, 7.5));
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

        return new Setup(user.Id, reviewer.Id, game.Id, material1.Id, material2.Id, criterion1.Id, criterion2.Id);
    }

    // Явно заданные оценки — как есть, остальные критерии базы — по 50 %.
    private async Task<Guid> CreateAsync(Setup s, Guid[] materials, Dictionary<int, byte> scores, NewMaterialInput[]? newMaterials = null)
    {
        await using var db = database.CreateContext();
        var input = new ReviewInput("Text", 8, materials, await TestData.AllCriteriaAsync(db, scores: scores), newMaterials);
        var review = await new ReviewService(db).CreateAsync(s.UserId, s.ReviewerId, input);

        return review.Id;
    }

    private static NewMaterialInput NewMaterial(Setup s) =>
        new(s.GameId, TestData.Unique("New material"), $"https://example.com/{Guid.NewGuid():N}", MaterialFormat.Article, new DateOnly(2026, 10, 1));

    // Оценки по двум критериям теста; остальные критерии базы заводят другие тесты.
    private static List<(int CriterionId, byte Score)> Scores(ReReviewer.Data.Entities.Review review, Setup s) =>
        review.CriterionScores
            .Where(c => c.CriterionId == s.Criterion1 || c.CriterionId == s.Criterion2)
            .OrderBy(c => c.CriterionId == s.Criterion1 ? 0 : 1)
            .Select(c => (c.CriterionId, c.Score))
            .ToList();
}
