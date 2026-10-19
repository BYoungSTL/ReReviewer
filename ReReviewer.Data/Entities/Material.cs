using ReReviewer.Data.Entities.BaseEntity;
using ReReviewer.Data.Entities.Enums;

namespace ReReviewer.Data.Entities;

public class Material : Entity<Guid>
{
    public Guid ReviewerId { get; set; }
    public Guid GameId { get; set; }
    public Guid? OutletId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public MaterialFormat Format { get; set; }

    // Оценка, которую обзорщик поставил игре, приведённая к шкале 0–10. Null — обзор без оценки.
    public decimal? OriginalScore { get; set; }

    public DateOnly? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    #region Virtual Properties

    public virtual Reviewer Reviewer { get; set; } = null!;
    public virtual Game Game { get; set; } = null!;
    public virtual Outlet? Outlet { get; set; }

    public virtual ICollection<Review> Reviews { get; set; } = [];

    #endregion
}