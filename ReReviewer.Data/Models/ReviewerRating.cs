namespace ReReviewer.Data.Models;

// Scores — сколько опубликованных отзывов поставили каждую оценку от 1 до 10, по порядку и без пропусков.
public record ReviewerRating(
    Guid ReviewerId,
    int ReviewCount,
    double? AverageScore,
    IReadOnlyList<CriterionRating> Criteria,
    IReadOnlyList<ScoreCount> Scores);

public record CriterionRating(
    int CriterionId,
    string Title,
    double AverageScore,
    int ScoreCount);

public record ScoreCount(
    byte Score,
    int Count);

// Сводка по опубликованным отзывам для списков обзорщиков.
public record ReviewerScoreSummary(
    Guid ReviewerId,
    int ReviewCount,
    double AverageScore);