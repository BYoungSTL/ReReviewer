namespace ReReviewer.Application.ViewModels;

// Связь «обзорщик — издание», только для чтения: привязка идёт через IOutletAppService.LinkReviewersAsync.
public class ReviewerOutletViewModel
{
    public Guid ReviewerId { get; set; }
    public string? ReviewerName { get; set; }

    public Guid OutletId { get; set; }
    public string? OutletName { get; set; }

    public DateOnly? StartedAt { get; set; }
    public DateOnly? EndedAt { get; set; }
}