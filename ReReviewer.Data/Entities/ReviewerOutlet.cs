namespace ReReviewer.Data.Entities;

public class ReviewerOutlet
{
    public Guid ReviewerId { get; set; }
    public Guid OutletId { get; set; }
    public DateOnly? StartedAt { get; set; }
    public DateOnly? EndedAt { get; set; }

    #region Virtual Properties

    public virtual Reviewer Reviewer { get; set; } = null!;
    public virtual Outlet Outlet { get; set; } = null!;

    #endregion
}