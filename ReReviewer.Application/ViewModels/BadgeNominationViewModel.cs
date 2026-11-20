namespace ReReviewer.Application.ViewModels;

// Номинация обзорщика на знак. Автора контроллер берёт из токена.
public class BadgeNominationViewModel
{
    public Guid ReviewerId { get; set; }
    public int BadgeId { get; set; }
}

// Строка очереди модерации знаков: пара «обзорщик + знак» и сколько пользователей её номинировали.
public class BadgeNominationSummaryViewModel
{
    public Guid ReviewerId { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public int BadgeId { get; set; }
    public string BadgeTitle { get; set; } = string.Empty;
    public int NominationCount { get; set; }
    public DateTime FirstNominatedAt { get; set; }
    public DateTime LastNominatedAt { get; set; }
}
