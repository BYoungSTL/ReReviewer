namespace ReReviewer.Application.ViewModels;

// Выданный обзорщику бейдж, только для чтения.
public class ReviewerBadgeViewModel
{
    public int BadgeId { get; set; }
    public string? BadgeTitle { get; set; }
    public DateTime AwardedAt { get; set; }
}