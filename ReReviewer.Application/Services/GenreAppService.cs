using ReReviewer.Application.Interfaces;
using ReReviewer.Application.Services.Base;
using ReReviewer.Application.ViewModels;
using ReReviewer.Data.Entities;
using ReReviewer.Data.Interfaces;

namespace ReReviewer.Application.Services;

public class GenreAppService(IGenreService genreService) : IGenreAppService
{
    public async Task<List<GenreViewModel>> GetGenresAsync()
    {
        var genres = await genreService.GetAllAsync();

        return genres.OrderBy(g => g.Title).Select(ToViewModel).ToList();
    }

    public async Task<GenreViewModel> GetGenreAsync(int genreId) =>
        ToViewModel(await genreService.GetRequiredAsync(genreId));

    public async Task<bool> SaveGenreAsync(GenreViewModel genre)
    {
        var title = InputValidation.RequiredText(genre.Title, "Genre title");

        var saved = await genreService.SaveAsync(genre.Id, g => g.Title = title);

        genre.Id = saved.Id;
        genre.Title = saved.Title;

        return true;
    }

    // Вместе с жанром пропадают его связи с играми, обзорщиками и заявками; сами они остаются.
    public async Task DeleteGenreAsync(int genreId) =>
        await genreService.DeleteRequiredAsync(genreId);

    private static GenreViewModel ToViewModel(Genre genre) =>
        new()
        {
            Id = genre.Id,
            Title = genre.Title
        };
}