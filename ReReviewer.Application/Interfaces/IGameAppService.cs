using ReReviewer.Application.ViewModels;

namespace ReReviewer.Application.Interfaces;

public interface IGameAppService
{
    public Task<List<GameViewModel>> GetGamesAsync();
    public Task<GameViewModel> GetGameAsync(Guid gameId);
    public Task<bool> SaveGameAsync(GameViewModel game);
    public Task DeleteGameAsync(Guid gameId);
}