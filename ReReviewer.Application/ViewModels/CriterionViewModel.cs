namespace ReReviewer.Application.ViewModels;

public class CriterionViewModel
{
    // Null — новый критерий, иначе — правка существующего.
    public int? Id { get; set; }

    public string Title { get; set; } = string.Empty;

    // Null — критерий подходит для любого жанра.
    public int? GenreId { get; set; }

    #region Только чтение

    public string? GenreTitle { get; set; }

    #endregion
}