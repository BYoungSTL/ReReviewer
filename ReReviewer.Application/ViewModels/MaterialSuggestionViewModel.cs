namespace ReReviewer.Application.ViewModels;

// Заявка на новый материал обзорщика. Поля с пометкой "только чтение" заполняет сервис.
public class MaterialSuggestionViewModel
{
    public Guid? Id { get; set; }

    public Guid ReviewerId { get; set; }
    public Guid GameId { get; set; }

    public string Title { get; set; } = string.Empty;

    // Адрес http или https.
    public string Url { get; set; } = string.Empty;

    // Article, Video, Stream или Podcast.
    public string Format { get; set; } = string.Empty;
    public DateOnly? PublishedAt { get; set; }

    #region Только чтение

    public string? ReviewerName { get; set; }
    public string? GameTitle { get; set; }
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public string? Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DecidedAt { get; set; }

    // Материал, созданный при одобрении.
    public Guid? MaterialId { get; set; }

    #endregion
}
