using System.ComponentModel.DataAnnotations;
using ReReviewer.Application.Services;
using ReReviewer.Application.ViewModels;
using ReReviewer.Data.Entities;
using ReReviewer.Data.Entities.Enums;
using ReReviewer.Data.Interfaces;

namespace ReReviewer.UnitTests.Services;

public class MaterialSuggestionAppServiceTests
{
    private readonly IMaterialSuggestionService _suggestions = Substitute.For<IMaterialSuggestionService>();
    private readonly MaterialSuggestionAppService _service;

    public MaterialSuggestionAppServiceTests() =>
        _service = new MaterialSuggestionAppService(_suggestions);

    [Fact]
    public async Task SuggestMaterialAsync_TrimsInput_AndParsesFormat()
    {
        var userId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        _suggestions.CreateAsync(default, default, default, default!, default!, default, default)
            .ReturnsForAnyArgs(new MaterialSuggestion
            {
                Title = "Review",
                Reviewer = new Reviewer { FullName = "Reviewer" },
                Game = new Game { Title = "Game" },
                User = new User { Name = "author" }
            });

        await _service.SuggestMaterialAsync(userId, Valid(reviewerId, gameId, title: "  Review  ", url: " https://example.com/review ", format: "video"));

        await _suggestions.Received(1).CreateAsync(userId, reviewerId, gameId, "Review", "https://example.com/review",
            MaterialFormat.Video, new DateOnly(2026, 11, 1), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://example.com/file")]
    [InlineData("example.com/review")]
    public async Task SuggestMaterialAsync_NotHttpLink_Throws(string url)
    {
        var exception = await Should.ThrowAsync<ValidationException>(
            () => _service.SuggestMaterialAsync(Guid.NewGuid(), Valid(Guid.NewGuid(), Guid.NewGuid(), url: url)));

        exception.Message.ShouldBe("Material link must be an http or https address.");
        await _suggestions.DidNotReceiveWithAnyArgs().CreateAsync(default, default, default, default!, default!, default, default);
    }

    [Fact]
    public async Task SuggestMaterialAsync_UnknownFormat_Throws()
    {
        var exception = await Should.ThrowAsync<ValidationException>(
            () => _service.SuggestMaterialAsync(Guid.NewGuid(), Valid(Guid.NewGuid(), Guid.NewGuid(), format: "Tweet")));

        exception.Message.ShouldStartWith("Unknown material format 'Tweet'.");
    }

    [Fact]
    public async Task SuggestMaterialAsync_TooLongTitle_Throws()
    {
        var exception = await Should.ThrowAsync<ValidationException>(
            () => _service.SuggestMaterialAsync(Guid.NewGuid(), Valid(Guid.NewGuid(), Guid.NewGuid(), title: new string('a', 251))));

        exception.Message.ShouldBe("Material title must be at most 250 characters long.");
    }

    private static MaterialSuggestionViewModel Valid(Guid reviewerId, Guid gameId, string title = "Review",
        string url = "https://example.com/review", string format = "Video") =>
        new()
        {
            ReviewerId = reviewerId,
            GameId = gameId,
            Title = title,
            Url = url,
            Format = format,
            PublishedAt = new DateOnly(2026, 11, 1)
        };
}
