using ReReviewer.Application.Services;
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
}
