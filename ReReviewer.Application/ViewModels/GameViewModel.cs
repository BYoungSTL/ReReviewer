namespace ReReviewer.Application.ViewModels;

public class GameViewModel
{
    // Null — новая игра, иначе — правка существующей.
    public Guid? Id { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    // При сохранении используется только Id жанров.
    public List<GenreViewModel> Genres { get; set; } = [];
}