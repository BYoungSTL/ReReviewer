using ReReviewer.Data.Entities.BaseEntity;
using ReReviewer.Data.Entities.Enums;

namespace ReReviewer.Data.Entities;

public class Review : Entity<Guid>
{
    public Guid UserId { get; set; }
    public Guid ReviewerId { get; set; }

    public string Text { get; set; } = string.Empty;
    public byte Score { get; set; }
    public ReviewStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    #region Virtual Properties

    public virtual User User { get; set; } = null!;
    public virtual Reviewer Reviewer { get; set; } = null!;

    // Материалы, на которые ссылается отзыв. Пустой список — отзыв о работе обзорщика в целом.
    public virtual ICollection<Material> Materials { get; set; } = [];

    public virtual ICollection<ReviewCriterionScore> CriterionScores { get; set; } = [];
    #endregion
}