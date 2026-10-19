using ReReviewer.Data.Entities.BaseEntity;

namespace ReReviewer.Data.Entities;

public class Criterion : Entity<int>
{
    public string Title { get; set; } = string.Empty;

    // Null — критерий подходит для любого жанра.
    public int? GenreId { get; set; }

    #region Virtual Properties

    public virtual Genre? Genre { get; set; }

    public virtual ICollection<ReviewCriterionScore> Scores { get; set; } = [];

    #endregion
}