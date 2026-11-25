namespace ReReviewer.Application.ViewModels;

// Материал, которого у обзорщика ещё нет: автор добавляет его прямо в отзыв. Проверяется вместе с отзывом —
// при одобрении отзыва становится материалом обзорщика и попадает в Materials отзыва.
public class ReviewNewMaterialViewModel
{
    public Guid GameId { get; set; }

    public string Title { get; set; } = string.Empty;

    // Адрес http или https.
    public string Url { get; set; } = string.Empty;

    // Article, Video, Stream или Podcast.
    public string Format { get; set; } = string.Empty;
    public DateOnly? PublishedAt { get; set; }

    #region Только чтение

    public string? GameTitle { get; set; }

    // Pending — ждёт решения вместе с отзывом, Rejected — отклонён вместе с ним.
    public string? Status { get; set; }

    #endregion
}
