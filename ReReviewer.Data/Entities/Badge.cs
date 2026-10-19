using ReReviewer.Data.Entities.BaseEntity;

namespace ReReviewer.Data.Entities;

public class Badge : Entity<int>
{
    public string Title { get; set; } = string.Empty;

    #region Virtual Properties

    public virtual ICollection<ReviewerBadge> Reviewers { get; set; } = [];

    #endregion
}