using ReReviewer.Data.Entities.BaseEntity;

namespace ReReviewer.Data.Entities;

public class Role : Entity<int>
{
    public string Title { get; set; } = string.Empty;

    #region Virtual Properties

    public virtual ICollection<User> Users { get; set; } = [];

    #endregion
}