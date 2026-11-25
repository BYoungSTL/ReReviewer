using System.ComponentModel.DataAnnotations;
using ReReviewer.Application.Services;
using ReReviewer.Application.ViewModels;
using ReReviewer.Data.Entities;
using ReReviewer.Data.Entities.Enums;
using ReReviewer.Data.Interfaces;
using ReReviewer.Data.Models;

namespace ReReviewer.UnitTests.Services;

public class ReviewAppServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ReviewerId = Guid.NewGuid();

    private readonly IReviewService _reviews = Substitute.For<IReviewService>();
    private readonly ReviewAppService _service;

    public ReviewAppServiceTests() =>
        _service = new ReviewAppService(_reviews);

    #region SaveReviewAsync

    [Fact]
    public async Task SaveReviewAsync_WithoutId_CreatesReview_AndWritesBackFields()
    {
        var materialId = Guid.NewGuid();
        var created = new Review { Id = Guid.NewGuid(), ReviewerId = ReviewerId, Status = ReviewStatus.Pending, CreatedAt = new DateTime(2026, 10, 1) };
        _reviews.CreateAsync(UserId, ReviewerId, Arg.Any<ReviewInput>(), Arg.Any<CancellationToken>()).Returns(created);
        var review = NewReview();
        review.Materials = [new() { Id = materialId }];
        review.CriterionScores = [new() { CriterionId = 1, Score = 7 }];

        (await _service.SaveReviewAsync(review)).ShouldBeTrue();

        review.Id.ShouldBe(created.Id);
        review.Status.ShouldBe("Pending");
        review.CreatedAt.ShouldBe(created.CreatedAt);
        await _reviews.Received(1).CreateAsync(UserId, ReviewerId, Arg.Is<ReviewInput>(i =>
            i.Text == "Текст" && i.Score == 8 && i.MaterialIds.Single() == materialId && i.CriterionScores[1] == 7), Arg.Any<CancellationToken>());
        await _reviews.DidNotReceiveWithAnyArgs().EditAsync(default, default, default!, default);
    }

    [Fact]
    public async Task SaveReviewAsync_WithId_EditsReview()
    {
        var reviewId = Guid.NewGuid();
        var edited = new Review { Id = reviewId, ReviewerId = ReviewerId, Status = ReviewStatus.Pending, UpdatedAt = DateTime.UtcNow };
        _reviews.EditAsync(reviewId, UserId, Arg.Any<ReviewInput>(), Arg.Any<CancellationToken>()).Returns(edited);
        var review = NewReview();
        review.Id = reviewId;

        await _service.SaveReviewAsync(review);

        review.UpdatedAt.ShouldBe(edited.UpdatedAt);
        await _reviews.DidNotReceiveWithAnyArgs().CreateAsync(default, default, default!, default);
    }

    [Fact]
    public async Task SaveReviewAsync_SameCriterionTwice_ThrowsBeforeCallingService()
    {
        var review = NewReview();
        review.CriterionScores = [new() { CriterionId = 1, Score = 7 }, new() { CriterionId = 1, Score = 3 }];

        var exception = await Should.ThrowAsync<ValidationException>(() => _service.SaveReviewAsync(review));

        exception.Message.ShouldBe("Each criterion can be scored only once.");
        await _reviews.DidNotReceiveWithAnyArgs().CreateAsync(default, default, default!, default);
    }

    [Fact]
    public async Task SaveReviewAsync_NewMaterials_AreTrimmedAndParsed()
    {
        var gameId = Guid.NewGuid();
        _reviews.CreateAsync(UserId, ReviewerId, Arg.Any<ReviewInput>(), Arg.Any<CancellationToken>()).Returns(new Review { Id = Guid.NewGuid() });
        var review = NewReview();
        review.NewMaterials = [new() { GameId = gameId, Title = "  Разбор  ", Url = " https://example.com/a ", Format = "podcast", PublishedAt = new DateOnly(2026, 9, 1) }];

        await _service.SaveReviewAsync(review);

        await _reviews.Received(1).CreateAsync(UserId, ReviewerId, Arg.Is<ReviewInput>(i =>
            i.NewMaterials!.Single() == new NewMaterialInput(gameId, "Разбор", "https://example.com/a", MaterialFormat.Podcast, new DateOnly(2026, 9, 1))),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("", "https://example.com/a", "Article", "Material title must not be empty.")]
    [InlineData("Разбор", "javascript:alert(1)", "Article", "Material link must be an http or https address.")]
    [InlineData("Разбор", "https://example.com/a", "Book", "Unknown material format 'Book'. Allowed: Article, Video, Stream, Podcast.")]
    public async Task SaveReviewAsync_InvalidNewMaterial_ThrowsBeforeCallingService(string title, string url, string format, string message)
    {
        var review = NewReview();
        review.NewMaterials = [new() { GameId = Guid.NewGuid(), Title = title, Url = url, Format = format }];

        var exception = await Should.ThrowAsync<ValidationException>(() => _service.SaveReviewAsync(review));

        exception.Message.ShouldBe(message);
        await _reviews.DidNotReceiveWithAnyArgs().CreateAsync(default, default, default!, default);
    }

    #endregion

    #region Чтение

    [Theory]
    [InlineData(1, 20, 0, 20, 1, 20)]
    [InlineData(3, 10, 20, 10, 3, 10)]
    [InlineData(0, 1000, 0, 100, 1, 100)]
    [InlineData(-5, 0, 0, 1, 1, 1)]
    public async Task GetReviewerReviewsAsync_NormalizesPaging(
        int page, int pageSize, int expectedSkip, int expectedTake, int expectedPage, int expectedPageSize)
    {
        _reviews.GetPublishedByReviewerAsync(ReviewerId, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Review>)[]);
        _reviews.CountPublishedByReviewerAsync(ReviewerId, Arg.Any<CancellationToken>()).Returns(42);

        var result = await _service.GetReviewerReviewsAsync(ReviewerId, page, pageSize);

        result.Page.ShouldBe(expectedPage);
        result.PageSize.ShouldBe(expectedPageSize);
        result.TotalCount.ShouldBe(42);
        result.TotalPages.ShouldBe((int)Math.Ceiling(42.0 / expectedPageSize));
        await _reviews.Received(1).GetPublishedByReviewerAsync(ReviewerId, expectedSkip, expectedTake, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetReviewAsync_MapsNavigationsAndSortsCollections()
    {
        var review = new Review
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            ReviewerId = ReviewerId,
            Text = "Текст",
            Score = 9,
            Status = ReviewStatus.Published,
            User = new User { Name = "player" },
            Reviewer = new Reviewer { FullName = "Обзорщик" },
            Materials = [new() { Title = "Б", Format = MaterialFormat.Video }, new() { Title = "А", Format = MaterialFormat.Article }],
            CriterionScores = [new() { CriterionId = 2, Score = 5, Criterion = new() { Title = "Подача" } }, new() { CriterionId = 1, Score = 9, Criterion = new() { Title = "Глубина" } }]
        };
        _reviews.GetByIdAsync(review.Id, Arg.Any<CancellationToken>()).Returns(review);

        var result = await _service.GetReviewAsync(review.Id);

        result.UserName.ShouldBe("player");
        result.ReviewerName.ShouldBe("Обзорщик");
        result.Status.ShouldBe("Published");
        result.Materials.Select(m => m.Title).ShouldBe(["А", "Б"]);
        result.Materials.Select(m => m.Format).ShouldBe(["Article", "Video"]);
        result.CriterionScores.Select(s => s.CriterionTitle).ShouldBe(["Глубина", "Подача"]);
    }

    // Одобренные вместе с отзывом материалы уже в Materials, поэтому в NewMaterials только неодобренные.
    [Fact]
    public async Task GetReviewAsync_NewMaterials_SkipApproved()
    {
        var game = new Game { Id = Guid.NewGuid(), Title = "Игра" };
        var review = new Review
        {
            Id = Guid.NewGuid(),
            User = new User { Name = "player" },
            Reviewer = new Reviewer { FullName = "Обзорщик" },
            MaterialSuggestions =
            [
                new() { Title = "Одобрен", Url = "https://example.com/a", Status = RequestStatus.Approved, Game = game },
                new() { GameId = game.Id, Title = "Ждёт", Url = "https://example.com/b", Format = MaterialFormat.Stream, Status = RequestStatus.Pending, Game = game }
            ]
        };
        _reviews.GetByIdAsync(review.Id, Arg.Any<CancellationToken>()).Returns(review);

        var result = await _service.GetReviewAsync(review.Id);

        var material = result.NewMaterials.ShouldHaveSingleItem();
        (material.GameId, material.Title, material.Url, material.Format, material.GameTitle, material.Status)
            .ShouldBe((game.Id, "Ждёт", "https://example.com/b", "Stream", "Игра", "Pending"));
    }

    [Fact]
    public async Task GetReviewAsync_Missing_ThrowsKeyNotFound()
    {
        _reviews.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Review?)null);

        (await Should.ThrowAsync<KeyNotFoundException>(() => _service.GetReviewAsync(Guid.NewGuid()))).Message.ShouldBe("Review not found.");
    }

    #endregion

    [Theory]
    [InlineData(true, ReviewStatus.Published)]
    [InlineData(false, ReviewStatus.Rejected)]
    public async Task ModerateReviewAsync_SetsStatus(bool approve, ReviewStatus expected)
    {
        var reviewId = Guid.NewGuid();

        await _service.ModerateReviewAsync(reviewId, approve);

        await _reviews.Received(1).SetStatusAsync(reviewId, expected, Arg.Any<CancellationToken>());
    }

    private static ReviewViewModel NewReview() =>
        new() { UserId = UserId, ReviewerId = ReviewerId, Text = "Текст", Score = 8 };
}