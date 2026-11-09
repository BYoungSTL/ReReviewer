using ReReviewer.Data.Entities.BaseEntity;
using ReReviewer.Data.Entities.Enums;

namespace ReReviewer.Data.Entities;

// Пользователь предлагает добавить обзорщика. В таблицу Reviewers он попадает только после одобрения модератором,
// поэтому непроверенные обзорщики не видны ни в одном публичном списке.
public class ReviewerSuggestion : Entity<Guid>
{
    public Guid UserId { get; set; }

    public string FullName { get; set; } = string.Empty;

    // Где обзорщик публикуется: ссылки на канал, издание и т. п. Для модератора.
    public string? Note { get; set; }

    // Пользователь предлагает себя: после одобрения профиль сразу привязывается к его аккаунту.
    public bool IsSelf { get; set; }

    public RequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DecidedAt { get; set; }

    // Профиль, созданный при одобрении. Без внешнего ключа: с ним у SQL Server было бы два каскадных пути
    // от Users (через автора заявки и через Reviewers.UserId). Если профиль удалят, ссылка просто устареет.
    public Guid? ReviewerId { get; set; }

    #region Virtual Properties

    public virtual User User { get; set; } = null!;
    public virtual ICollection<Genre> Genres { get; set; } = [];

    #endregion
}
