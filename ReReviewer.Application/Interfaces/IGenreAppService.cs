using ReReviewer.Application.ViewModels;

namespace ReReviewer.Application.Interfaces;

public interface IGenreAppService
{
    public Task<List<GenreViewModel>> GetGenresAsync();
    public Task<GenreViewModel> GetGenreAsync(int genreId);
    public Task<bool> SaveGenreAsync(GenreViewModel genre);
    public Task DeleteGenreAsync(int genreId);
}