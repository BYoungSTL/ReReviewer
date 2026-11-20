namespace ReReviewer.Application.ViewModels;

// Заявка на нового обзорщика. Поля с пометкой "только чтение" заполняет сервис, при создании они игнорируются.
public class ReviewerSuggestionViewModel
{
    public Guid? Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    // Где обзорщик публикуется: ссылки на канал, издание. Необязательно.
    public string? Note { get; set; }

    // Автор предлагает себя: после одобрения профиль привяжется к его аккаунту.
    public bool IsSelf { get; set; }

    // При создании используется только Id жанров, нужен хотя бы один.
    public List<GenreViewModel> Genres { get; set; } = [];

    #region Только чтение

    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public string? Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DecidedAt { get; set; }

    // Профиль, созданный при одобрении.
    public Guid? ReviewerId { get; set; }

    #endregion
}
