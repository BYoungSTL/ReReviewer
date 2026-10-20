namespace ReReviewer.Application.ViewModels;

// Одна модель и для чтения, и для сохранения. Поля с пометкой "только чтение"
// заполняет сервис, при сохранении они игнорируются.
public class ReviewViewModel
{
    // Null — новый отзыв, иначе — правка существующего.
    public Guid? Id { get; set; }

    // Автора контроллер должен брать из токена, а не из тела запроса.
    public Guid UserId { get; set; }
    public Guid ReviewerId { get; set; }

    public string Text { get; set; } = string.Empty;
    public byte Score { get; set; }

    // Пустой список — отзыв о работе обзорщика в целом. При сохранении используется только Id.
    public List<ReviewMaterialViewModel> Materials { get; set; } = [];
    public List<ReviewCriterionScoreViewModel> CriterionScores { get; set; } = [];

    #region Только чтение

    public string? UserName { get; set; }
    public string? ReviewerName { get; set; }
    public string? Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    #endregion
}