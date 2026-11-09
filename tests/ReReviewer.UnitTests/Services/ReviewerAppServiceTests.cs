using System.ComponentModel.DataAnnotations;
using ReReviewer.Application.Services;
using ReReviewer.Application.ViewModels;
using ReReviewer.Data.Entities;
using ReReviewer.Data.Interfaces;
using ReReviewer.Data.Models;

namespace ReReviewer.UnitTests.Services;

public class ReviewerAppServiceTests
{
    private readonly IReviewerService _reviewers = Substitute.For<IReviewerService>();
    private readonly IReviewService _reviews = Substitute.For<IReviewService>();
    private readonly ReviewerAppService _service;

    public ReviewerAppServiceTests() =>
        _service = new ReviewerAppService(_reviewers, Substitute.For<IGenreService>(), _reviews);

    [Fact]
    public async Task GetReviewersAsync_AddsRatingFromPublishedSummaries()
    {
        Reviewer rated = new() { Id = Guid.NewGuid(), FullName = "Rated" }, unrated = new() { Id = Guid.NewGuid(), FullName = "Unrated" };
        _reviewers.GetAllAsync(Arg.Any<CancellationToken>()).Returns((IReadOnlyList<Reviewer>)[rated, unrated]);
        _reviews.GetPublishedSummariesAsync(null, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyDictionary<Guid, ReviewerScoreSummary>)new Dictionary<Guid, ReviewerScoreSummary>
            {
                [rated.Id] = new(rated.Id, 3, 7.5)
            });

        var result = await _service.GetReviewersAsync();

        var withReviews = result.Single(r => r.Id == rated.Id);
        withReviews.ReviewCount.ShouldBe(3);
        withReviews.AverageScore.ShouldBe(7.5);
        var withoutReviews = result.Single(r => r.Id == unrated.Id);
        withoutReviews.ReviewCount.ShouldBe(0);
        withoutReviews.AverageScore.ShouldBeNull();
    }

    [Fact]
    public async Task GetReviewerAsync_AsksSummaryOnlyForThisReviewer()
    {
        var reviewer = new Reviewer { Id = Guid.NewGuid(), FullName = "Rated" };
        _reviewers.GetByIdAsync(reviewer.Id, Arg.Any<CancellationToken>()).Returns(reviewer);
        _reviews.GetPublishedSummariesAsync(Arg.Any<IReadOnlyCollection<Guid>?>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyDictionary<Guid, ReviewerScoreSummary>)new Dictionary<Guid, ReviewerScoreSummary>
            {
                [reviewer.Id] = new(reviewer.Id, 2, 9)
            });

        var result = await _service.GetReviewerAsync(reviewer.Id);

        result.AverageScore.ShouldBe(9);
        await _reviews.Received(1).GetPublishedSummariesAsync(
            Arg.Is<IReadOnlyCollection<Guid>?>(ids => ids != null && ids.SequenceEqual(new[] { reviewer.Id })),
            Arg.Any<CancellationToken>());
    }

    #region Свой профиль

    [Fact]
    public async Task UpdateOwnProfileAsync_TrimsText_AndDropsEmptyAndDuplicateLinks()
    {
        var userId = Guid.NewGuid();
        _reviewers.UpdateOwnProfileAsync(default, default!, default).ReturnsForAnyArgs(new Reviewer { Id = Guid.NewGuid(), FullName = "Name" });
        _reviews.GetPublishedSummariesAsync(Arg.Any<IReadOnlyCollection<Guid>?>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyDictionary<Guid, ReviewerScoreSummary>)new Dictionary<Guid, ReviewerScoreSummary>());

        await _service.UpdateOwnProfileAsync(userId, new ReviewerProfileViewModel
        {
            FullName = "  Name  ",
            About = "   ",
            Links = [" https://example.com/a ", "", "https://EXAMPLE.com/a", "https://example.com/b"],
            Genres = [new GenreViewModel { Id = 2 }]
        });

        await _reviewers.Received(1).UpdateOwnProfileAsync(userId, Arg.Is<ReviewerProfileInput>(input =>
            input.FullName == "Name"
            && input.About == null
            && input.Links.SequenceEqual(new[] { "https://example.com/a", "https://example.com/b" })
            && input.GenreIds.SequenceEqual(new[] { 2 })), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateOwnProfileAsync_CountsLineBreakAsOneCharacter()
    {
        _reviewers.UpdateOwnProfileAsync(default, default!, default).ReturnsForAnyArgs(new Reviewer { Id = Guid.NewGuid(), FullName = "Name" });
        _reviews.GetPublishedSummariesAsync(Arg.Any<IReadOnlyCollection<Guid>?>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyDictionary<Guid, ReviewerScoreSummary>)new Dictionary<Guid, ReviewerScoreSummary>());
        // 1001 символ с \r\n, но ровно 1000 с одним \n — столько разрешает поле на сайте.
        var about = new string('a', 499) + "\r\n" + new string('b', 500);

        await _service.UpdateOwnProfileAsync(Guid.NewGuid(),
            new ReviewerProfileViewModel { FullName = "Name", About = about, Genres = [new GenreViewModel { Id = 1 }] });

        await _reviewers.Received(1).UpdateOwnProfileAsync(Arg.Any<Guid>(), Arg.Is<ReviewerProfileInput>(input =>
            input.About == new string('a', 499) + "\n" + new string('b', 500)), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("javascript:alert(1)", "Links must be http or https addresses.")]
    [InlineData("example.com/no-scheme", "Links must be http or https addresses.")]
    public async Task UpdateOwnProfileAsync_UnsafeLink_Throws(string link, string message)
    {
        var exception = await Should.ThrowAsync<ValidationException>(() => _service.UpdateOwnProfileAsync(Guid.NewGuid(),
            new ReviewerProfileViewModel { FullName = "Name", Links = [link], Genres = [new GenreViewModel { Id = 1 }] }));

        exception.Message.ShouldBe(message);
        await _reviewers.DidNotReceiveWithAnyArgs().UpdateOwnProfileAsync(default, default!, default);
    }

    [Fact]
    public async Task UpdateOwnProfileAsync_TooManyLinks_Throws()
    {
        var links = Enumerable.Range(1, 6).Select(i => $"https://example.com/{i}").ToList();

        var exception = await Should.ThrowAsync<ValidationException>(() => _service.UpdateOwnProfileAsync(Guid.NewGuid(),
            new ReviewerProfileViewModel { FullName = "Name", Links = links, Genres = [new GenreViewModel { Id = 1 }] }));

        exception.Message.ShouldBe("At most 5 links are allowed.");
    }

    #endregion
}
