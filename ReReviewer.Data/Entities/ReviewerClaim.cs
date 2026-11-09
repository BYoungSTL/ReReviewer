using ReReviewer.Data.Entities.BaseEntity;
using ReReviewer.Data.Entities.Enums;

namespace ReReviewer.Data.Entities;

// Запрос пользователя привязать существующий профиль обзорщика к своему аккаунту («это я»).
// Модератор проверяет доказательство из Note и одобряет запрос или отклоняет его.
public class ReviewerClaim : Entity<Guid>
{
    public Guid UserId { get; set; }
    public Guid ReviewerId { get; set; }

    // Как проверить, что это он: ссылка на пост или видео, рабочая почта и т. п.
    public string Note { get; set; } = string.Empty;

    public RequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DecidedAt { get; set; }

    #region Virtual Properties

    public virtual User User { get; set; } = null!;
    public virtual Reviewer Reviewer { get; set; } = null!;

    #endregion
}
