using ReReviewer.Data.Entities.BaseEntity;

namespace ReReviewer.Data.Entities;

public class User : Entity<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Null — аккаунт активен. Заблокированный пользователь не может войти, а выданные ему токены не принимаются.
    public DateTime? BlockedAt { get; set; }

    // Токены, выданные раньше смены пароля, не принимаются: смена пароля завершает остальные сеансы.
    public DateTime? PasswordChangedAt { get; set; }

    // Когда загружена аватарка; null — аватарки нет. Сама картинка — в UserAvatars, чтобы не грузить её с пользователем.
    public DateTime? AvatarUpdatedAt { get; set; }

    #region Virtual Properties

    public virtual ICollection<Role> Roles { get; set; } = [];

    public virtual Reviewer? Reviewer { get; set; }
    
    public virtual ICollection<Review> Reviews { get; set; } = [];
    #endregion
}