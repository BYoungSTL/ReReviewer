using ReReviewer.Data.Entities.BaseEntity;

namespace ReReviewer.Data.Entities;

public class Outlet : Entity<Guid>
{
    public string Name { get; set; } = string.Empty;

    #region Virtual Properties

    public virtual ICollection<ReviewerOutlet> Reviewers { get; set; } = [];
    public virtual ICollection<Material> Materials { get; set; } = [];

    #endregion
}