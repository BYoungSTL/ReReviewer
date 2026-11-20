namespace ReReviewer.Data.Models;

// Строка очереди модерации знаков: сколько пользователей номинировали обзорщика на знак и когда.
public record BadgeNominationSummary(
    Guid ReviewerId,
    string ReviewerName,
    int BadgeId,
    string BadgeTitle,
    int NominationCount,
    DateTime FirstNominatedAt,
    DateTime LastNominatedAt);
