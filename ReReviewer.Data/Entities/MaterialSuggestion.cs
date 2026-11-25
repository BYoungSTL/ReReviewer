using ReReviewer.Data.Entities.BaseEntity;
using ReReviewer.Data.Entities.Enums;

namespace ReReviewer.Data.Entities;

// Пользователь предлагает материал обзорщика (статью, видео, стрим, подкаст). В таблицу Materials он попадает только
// после одобрения модератором: до этого его не видно на странице обзорщика и нельзя отметить в отзыве.
// Заявка бывает отдельной (со страницы обзорщика) или частью отзыва (ReviewId): тогда её решают вместе с отзывом —
// одобрение отзыва создаёт материал и отмечает его в отзыве, отклонение отзыва отклоняет и заявку.
public class MaterialSuggestion : Entity<Guid>
{
    public Guid UserId { get; set; }
    public Guid ReviewerId { get; set; }
    public Guid GameId { get; set; }
    public Guid? ReviewId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public MaterialFormat Format { get; set; }
    public DateOnly? PublishedAt { get; set; }

    public RequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DecidedAt { get; set; }

    // Материал, созданный при одобрении. Без внешнего ключа, как ReviewerSuggestion.ReviewerId: если материал удалят,
    // ссылка просто устареет.
    public Guid? MaterialId { get; set; }

    #region Virtual Properties

    public virtual User User { get; set; } = null!;
    public virtual Reviewer Reviewer { get; set; } = null!;
    public virtual Game Game { get; set; } = null!;
    public virtual Review? Review { get; set; }

    #endregion
}
