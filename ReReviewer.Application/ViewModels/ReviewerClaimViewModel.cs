namespace ReReviewer.Application.ViewModels;

// Запрос привязать профиль обзорщика к своему аккаунту. Поля с пометкой "только чтение" заполняет сервис.
public class ReviewerClaimViewModel
{
    public Guid? Id { get; set; }
    public Guid ReviewerId { get; set; }

    // Как модератору убедиться, что это он: ссылка на пост или видео, рабочая почта. Обязательно.
    public string Note { get; set; } = string.Empty;

    #region Только чтение

    public string? ReviewerName { get; set; }
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public string? Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DecidedAt { get; set; }

    #endregion
}
