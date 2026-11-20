using ReReviewer.Data.Entities.BaseEntity;

namespace ReReviewer.Data.Entities;

// Пользователь считает, что обзорщик заслуживает знак. Модератор видит, сколько номинаций набрала пара
// «обзорщик + знак», и выдаёт знак (ReviewerBadge) или отклоняет номинации; в обоих случаях они удаляются.
// Один пользователь номинирует обзорщика на один знак только раз.
public class BadgeNomination : Entity<Guid>
{
    public Guid UserId { get; set; }
    public Guid ReviewerId { get; set; }
    public int BadgeId { get; set; }
    public DateTime CreatedAt { get; set; }

    #region Virtual Properties

    public virtual User User { get; set; } = null!;
    public virtual Reviewer Reviewer { get; set; } = null!;
    public virtual Badge Badge { get; set; } = null!;

    #endregion
}
