using ReReviewer.Application.Interfaces;
using ReReviewer.Application.Services.Base;
using ReReviewer.Application.ViewModels;
using ReReviewer.Data.Entities;
using ReReviewer.Data.Interfaces;

namespace ReReviewer.Application.Services;

// Жанры игры подгружаются лениво при сборке ViewModel.
public class GameAppService(IGameService gameService, IGenreService genreService) : IGameAppService
{
    public async Task<List<GameViewModel>> GetGamesAsync()
    {
        var games = await gameService.GetAllAsync();

        return games.OrderBy(g => g.Title).Select(ToViewModel).ToList();
    }

    public async Task<GameViewModel> GetGameAsync(Guid gameId) =>
        ToViewModel(await gameService.GetRequiredAsync(gameId));

    public async Task<bool> SaveGameAsync(GameViewModel game)
    {
        var title = InputValidation.RequiredText(game.Title, "Game title");
        var genreIds = InputValidation.RequiredIds(game.Genres, g => g.Id, "Genre");
        var genres = await genreService.GetByIdsAsync(genreIds);

        var saved = await gameService.SaveAsync(game.Id, g =>
        {
            g.Title = title;
            g.Description = InputValidation.OptionalText(game.Description);
            g.Genres.SyncWith(genres);
        });

        game.Id = saved.Id;
        game.Title = saved.Title;
        game.Description = saved.Description;

        return true;
    }

    // Игру, на которую есть материалы, удалить нельзя (ограничение базы).
    public async Task DeleteGameAsync(Guid gameId) =>
        await gameService.DeleteRequiredAsync(gameId);

    private static GameViewModel ToViewModel(Game game) =>
        new()
        {
            Id = game.Id,
            Title = game.Title,
            Description = game.Description,
            Genres = game.Genres
                .OrderBy(g => g.Title)
                .Select(g => new GenreViewModel { Id = g.Id, Title = g.Title })
                .ToList()
        };
}