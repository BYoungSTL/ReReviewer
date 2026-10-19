namespace ReReviewer.Data.Entities;

public class ReviewerBadge
{
    public Guid ReviewerId { get; set; }
    public int BadgeId { get; set; }
    public DateTime AwardedAt { get; set; }

    #region Virtual Properties

    public virtual Reviewer Reviewer { get; set; } = null!;
    public virtual Badge Badge { get; set; } = null!;

    #endregion
}