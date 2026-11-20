using ReReviewer.Data.Entities.BaseEntity;

namespace ReReviewer.Data.Entities;

// Аватарка пользователя, Id совпадает с Id пользователя. Отдельная таблица, чтобы байты картинки
// не загружались каждый раз вместе с пользователем: у User нет навигации сюда.
public class UserAvatar : Entity<Guid>
{
    public byte[] Content { get; set; } = [];
    public string ContentType { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}
