using System.ComponentModel.DataAnnotations;
using ReReviewer.Application.Services;
using ReReviewer.Application.ViewModels;
using ReReviewer.Data.Entities;
using ReReviewer.Data.Interfaces;

namespace ReReviewer.UnitTests.Services;

public class ReviewerRequestAppServiceTests
{
    private readonly IReviewerSuggestionService _suggestions = Substitute.For<IReviewerSuggestionService>();
    private readonly IReviewerClaimService _claims = Substitute.For<IReviewerClaimService>();
    private readonly ReviewerRequestAppService _service;

    public ReviewerRequestAppServiceTests() =>
        _service = new ReviewerRequestAppService(_suggestions, _claims);

    [Fact]
    public async Task SuggestReviewerAsync_TrimsInput_AndPassesGenreIds()
    {
        var userId = Guid.NewGuid();
        _suggestions.CreateAsync(default, default!, default, default, default!, default)
            .ReturnsForAnyArgs(new ReviewerSuggestion { FullName = "Name", User = new User { Name = "author" } });

        await _service.SuggestReviewerAsync(userId, new ReviewerSuggestionViewModel
        {
            FullName = "  Name  ",
            Note = "   ",
            IsSelf = true,
            Genres = [new GenreViewModel { Id = 3 }, new GenreViewModel { Id = 5 }]
        });

        await _suggestions.Received(1).CreateAsync(userId, "Name", null, true,
            Arg.Is<IReadOnlyCollection<int>>(ids => ids.SequenceEqual(new[] { 3, 5 })), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SuggestReviewerAsync_EmptyName_Throws(string name)
    {
        var exception = await Should.ThrowAsync<ValidationException>(
            () => _service.SuggestReviewerAsync(Guid.NewGuid(), new ReviewerSuggestionViewModel { FullName = name }));

        exception.Message.ShouldBe("Reviewer name must not be empty.");
    }

    [Fact]
    public async Task SuggestReviewerAsync_TooLongNote_Throws()
    {
        var exception = await Should.ThrowAsync<ValidationException>(() => _service.SuggestReviewerAsync(Guid.NewGuid(),
            new ReviewerSuggestionViewModel { FullName = "Name", Note = new string('x', 1001), Genres = [new GenreViewModel { Id = 1 }] }));

        exception.Message.ShouldBe("Note must be at most 1000 characters long.");
        await _suggestions.DidNotReceiveWithAnyArgs().CreateAsync(default, default!, default, default, default!, default);
    }

    [Fact]
    public async Task ClaimReviewerAsync_EmptyProof_Throws()
    {
        var exception = await Should.ThrowAsync<ValidationException>(
            () => _service.ClaimReviewerAsync(Guid.NewGuid(), new ReviewerClaimViewModel { ReviewerId = Guid.NewGuid(), Note = " " }));

        exception.Message.ShouldBe("Proof must not be empty.");
        await _claims.DidNotReceiveWithAnyArgs().CreateAsync(default, default, default!, default);
    }
}
