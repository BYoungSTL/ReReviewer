namespace ReReviewer.Data.Models;

public record ReviewerRating(
    Guid ReviewerId,
    int ReviewCount,
    double? AverageScore,
    IReadOnlyList<CriterionRating> Criteria);

public record CriterionRating(
    int CriterionId,
    string Title,
    double AverageScore,
    int ScoreCount);