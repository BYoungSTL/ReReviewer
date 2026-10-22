namespace ReReviewer.Application.ViewModels;

public class MaterialViewModel
{
    // Null — новый материал, иначе — правка существующего.
    public Guid? Id { get; set; }

    // Обзорщик задаётся только при создании: материал нельзя перенести к другому обзорщику.
    public Guid ReviewerId { get; set; }
    public Guid GameId { get; set; }
    public Guid? OutletId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;

    // Article, Video, Stream или Podcast.
    public string Format { get; set; } = string.Empty;

    // Оценка обзорщика игре по шкале 0–10. Null — обзор без оценки.
    public decimal? OriginalScore { get; set; }
    public DateOnly? PublishedAt { get; set; }

    #region Только чтение

    public string? ReviewerName { get; set; }
    public string? GameTitle { get; set; }
    public string? OutletName { get; set; }
    public DateTime CreatedAt { get; set; }

    #endregion
}