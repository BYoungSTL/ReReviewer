using System.ComponentModel.DataAnnotations;
using ReReviewer.Application.Services;
using ReReviewer.Application.ViewModels;
using ReReviewer.Data.Entities;
using ReReviewer.Data.Entities.Enums;
using ReReviewer.Data.Interfaces;

namespace ReReviewer.UnitTests.Services;

public class MaterialAppServiceTests
{
    private readonly IMaterialService _materials = Substitute.For<IMaterialService>();
    private readonly IReviewerService _reviewers = Substitute.For<IReviewerService>();
    private readonly IGameService _games = Substitute.For<IGameService>();
    private readonly IOutletService _outlets = Substitute.For<IOutletService>();
    private readonly MaterialAppService _service;

    private readonly Reviewer _reviewer = new() { Id = Guid.NewGuid(), FullName = "Обзорщик" };
    private readonly Game _game = new() { Id = Guid.NewGuid(), Title = "Elden Ring" };

    public MaterialAppServiceTests()
    {
        _service = new MaterialAppService(_materials, _reviewers, _games, _outlets);
        _reviewers.GetByIdAsync(_reviewer.Id, Arg.Any<CancellationToken>()).Returns(_reviewer);
        _games.GetByIdAsync(_game.Id, Arg.Any<CancellationToken>()).Returns(_game);
        _materials.AddAsync(Arg.Any<Material>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Material>());
    }

    [Theory]
    [InlineData("video", MaterialFormat.Video)]
    [InlineData("ARTICLE", MaterialFormat.Article)]
    [InlineData("Podcast", MaterialFormat.Podcast)]
    public async Task SaveMaterialAsync_ParsesFormatIgnoringCase(string format, MaterialFormat expected)
    {
        await _service.SaveMaterialAsync(Valid(format: format));

        await _materials.Received(1).AddAsync(Arg.Is<Material>(m => m.Format == expected), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("Tiktok")]
    [InlineData("")]
    public async Task SaveMaterialAsync_UnknownFormat_ThrowsBeforeTouchingData(string format)
    {
        var exception = await Should.ThrowAsync<ValidationException>(() => _service.SaveMaterialAsync(Valid(format: format)));

        exception.Message.ShouldStartWith($"Unknown material format '{format}'.");
        await _games.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
    }

    [Theory]
    [InlineData(-0.5)]
    [InlineData(10.5)]
    public async Task SaveMaterialAsync_ScoreOutOfRange_Throws(double score)
    {
        var exception = await Should.ThrowAsync<ValidationException>(() => _service.SaveMaterialAsync(Valid(score: (decimal)score)));

        exception.Message.ShouldBe("Original score must be between 0 and 10.");
    }

    [Fact]
    public async Task SaveMaterialAsync_UnknownGame_ThrowsKeyNotFound()
    {
        var material = Valid();
        material.GameId = Guid.NewGuid();

        var exception = await Should.ThrowAsync<KeyNotFoundException>(() => _service.SaveMaterialAsync(material));

        exception.Message.ShouldBe("Game not found.");
    }

    // Материал нельзя перенести к другому обзорщику: на него могут ссылаться отзывы о прежнем.
    [Fact]
    public async Task SaveMaterialAsync_Update_KeepsReviewer()
    {
        var existing = new Material { Id = Guid.NewGuid(), ReviewerId = _reviewer.Id, GameId = _game.Id, Title = "Old", Url = "https://example.com/a", Format = MaterialFormat.Video };
        _materials.GetByIdAsync(existing.Id, Arg.Any<CancellationToken>()).Returns(existing);
        var update = Valid(title: "New");
        update.Id = existing.Id;
        update.ReviewerId = Guid.NewGuid();

        await _service.SaveMaterialAsync(update);

        existing.ReviewerId.ShouldBe(_reviewer.Id);
        existing.Title.ShouldBe("New");
        update.ReviewerId.ShouldBe(_reviewer.Id);
        await _materials.Received(1).UpdateAsync(existing, Arg.Any<CancellationToken>());
    }

    private MaterialViewModel Valid(string format = "Video", decimal? score = 8, string title = "Обзор") =>
        new()
        {
            ReviewerId = _reviewer.Id,
            GameId = _game.Id,
            Title = title,
            Url = "https://example.com/a",
            Format = format,
            OriginalScore = score
        };
}