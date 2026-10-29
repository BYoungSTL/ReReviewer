using ReReviewer.Application.Services.Base;
using ReReviewer.Data.Entities;
using ReReviewer.Data.Interfaces;

namespace ReReviewer.UnitTests.Services;

public class AppServiceExtensionsTests
{
    private readonly IGenreService _genres = Substitute.For<IGenreService>();

    public AppServiceExtensionsTests() =>
        _genres.AddAsync(Arg.Any<Genre>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Genre>());

    #region SaveAsync

    [Fact]
    public async Task SaveAsync_WithoutId_AddsNewEntity()
    {
        var saved = await _genres.SaveAsync<Genre, int>(null, g => g.Title = "RPG");

        saved.Title.ShouldBe("RPG");
        await _genres.Received(1).AddAsync(Arg.Is<Genre>(g => g.Title == "RPG"), Arg.Any<CancellationToken>());
        await _genres.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
    }

    [Fact]
    public async Task SaveAsync_WithId_UpdatesExistingEntity()
    {
        var existing = new Genre { Id = 5, Title = "Old" };
        _genres.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(existing);

        var saved = await _genres.SaveAsync<Genre, int>(5, g => g.Title = "New");

        saved.ShouldBeSameAs(existing);
        existing.Title.ShouldBe("New");
        await _genres.Received(1).UpdateAsync(existing, Arg.Any<CancellationToken>());
        await _genres.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task SaveAsync_UnknownId_ThrowsKeyNotFound()
    {
        _genres.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns((Genre?)null);

        var exception = await Should.ThrowAsync<KeyNotFoundException>(() => _genres.SaveAsync<Genre, int>(5, g => g.Title = "New"));

        exception.Message.ShouldBe("Genre not found.");
    }

    #endregion

    #region GetRequiredAsync / DeleteRequiredAsync

    [Fact]
    public async Task GetRequiredAsync_Missing_ThrowsKeyNotFound()
    {
        _genres.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns((Genre?)null);

        await Should.ThrowAsync<KeyNotFoundException>(() => _genres.GetRequiredAsync(7));
    }

    [Fact]
    public async Task DeleteRequiredAsync_Missing_ThrowsKeyNotFound()
    {
        _genres.DeleteAsync(7, Arg.Any<CancellationToken>()).Returns(false);

        await Should.ThrowAsync<KeyNotFoundException>(() => _genres.DeleteRequiredAsync(7));
    }

    [Fact]
    public async Task DeleteRequiredAsync_Existing_Deletes()
    {
        _genres.DeleteAsync(7, Arg.Any<CancellationToken>()).Returns(true);

        await _genres.DeleteRequiredAsync(7);

        await _genres.Received(1).DeleteAsync(7, Arg.Any<CancellationToken>());
    }

    #endregion

    #region SyncWith

    [Fact]
    public void SyncWith_RemovesExtraAndAddsMissing()
    {
        Genre rpg = new() { Id = 1 }, action = new() { Id = 2 }, indie = new() { Id = 3 };
        var current = new List<Genre> { rpg, action };

        current.SyncWith([action, indie]);

        current.ShouldBe([action, indie], ignoreOrder: true);
    }

    [Fact]
    public void SyncWith_SameItems_KeepsSameInstances()
    {
        Genre rpg = new() { Id = 1 }, action = new() { Id = 2 };
        var current = new List<Genre> { rpg, action };

        current.SyncWith([rpg, action]);

        current.ShouldBe([rpg, action]);
    }

    [Fact]
    public void SyncWith_EmptyDesired_ClearsCollection()
    {
        var current = new List<Genre> { new() { Id = 1 } };

        current.SyncWith([]);

        current.ShouldBeEmpty();
    }

    #endregion
}