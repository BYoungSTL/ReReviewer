using ReReviewer.Data.Entities.BaseEntity;

namespace ReReviewer.Data.Entities;

public class Game : Entity<Guid>
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    #region Virtual Properties

    public virtual ICollection<Genre> Genres { get; set; } = [];
    public virtual ICollection<Material> Materials { get; set; } = [];

    #endregion
}