using ReReviewer.Data.Entities.BaseEntity;

namespace ReReviewer.Data.Entities;

public class User : Entity<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    #region Virtual Properties

    public virtual ICollection<Role> Roles { get; set; } = [];

    public virtual Reviewer? Reviewer { get; set; }
    
    public virtual ICollection<Review> Reviews { get; set; } = [];
    #endregion
}