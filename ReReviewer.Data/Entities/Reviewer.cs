using ReReviewer.Data.Entities.BaseEntity;

namespace ReReviewer.Data.Entities;

public class Reviewer : Entity<Guid>
{
    public string FullName { get; set; } = string.Empty;

    // Заполняется, когда обзорщик заявил права на свой профиль.
    public Guid? UserId { get; set; }

    #region Virtual Properties

    public virtual User? User { get; set; }

    public virtual ICollection<Material> Materials { get; set; } = [];
    public virtual ICollection<Review> Reviews { get; set; } = [];

    public virtual ICollection<ReviewerOutlet> Outlets { get; set; } = [];
    public virtual ICollection<ReviewerBadge> Badges { get; set; } = [];
    public virtual ICollection<Genre> Genres { get; set; } = [];

    #endregion
}