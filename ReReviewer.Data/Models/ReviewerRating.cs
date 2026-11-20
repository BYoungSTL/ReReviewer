namespace ReReviewer.Data.Models;

// Scores — сколько опубликованных отзывов поставили каждую оценку от 1 до 10, по порядку и без пропусков.
public record ReviewerRating(
    Guid ReviewerId,
    int ReviewCount,
    double? AverageScore,
    IReadOnlyList<CriterionRating> Criteria,
    IReadOnlyList<ScoreCount> Scores);

// AverageScore — средний процент (0–100) по опубликованным отзывам; null — по критерию ещё нет оценок.
// Критерии обязательны в каждом отзыве, поэтому в рейтинге есть все, даже без оценок.
public record CriterionRating(
    int CriterionId,
    string Title,
    double? AverageScore,
    int ScoreCount);

public record ScoreCount(
    byte Score,
    int Count);

// Сводка по опубликованным отзывам для списков обзорщиков.
public record ReviewerScoreSummary(
    Guid ReviewerId,
    int ReviewCount,
    double AverageScore);