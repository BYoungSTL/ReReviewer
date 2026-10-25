namespace ReReviewer.Application.ViewModels;

// Во вложенных списках (жанры игры, обзорщика) при сохранении используется только Id.
public class GenreViewModel
{
    // Null — новый жанр, иначе — правка существующего.
    public int? Id { get; set; }

    public string Title { get; set; } = string.Empty;
}