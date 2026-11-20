namespace ReReviewer.Application.ViewModels;

public class ReviewerRatingViewModel
{
    public Guid ReviewerId { get; set; }
    public int ReviewCount { get; set; }
    public double? AverageScore { get; set; }
    public List<CriterionRatingViewModel> Criteria { get; set; } = [];

    // Сколько отзывов поставили каждую оценку: всегда десять элементов, от 1 до 10.
    public List<ScoreCountViewModel> Scores { get; set; } = [];
}

public class ScoreCountViewModel
{
    public int Score { get; set; }
    public int Count { get; set; }
}

public class CriterionRatingViewModel
{
    public int CriterionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public double AverageScore { get; set; }
    public int ScoreCount { get; set; }
}