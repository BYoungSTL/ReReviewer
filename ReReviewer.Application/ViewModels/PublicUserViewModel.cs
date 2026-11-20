namespace ReReviewer.Application.ViewModels;

// Публичная страница пользователя: имя и аватарка (их и так видно у его отзывов) и профиль обзорщика,
// к которому модератор привязал аккаунт. Почты, ролей и блокировки здесь нет — это UserViewModel для администратора.
public class PublicUserViewModel
{
    public Guid Id { get; set; }

    // У привязанного аккаунта — имя его профиля обзорщика.
    public string Name { get; set; } = string.Empty;

    // Null — аватарки нет. Иначе картинка — api/users/{id}/avatar, а время годится как версия для её адреса.
    public DateTime? AvatarUpdatedAt { get; set; }

    // Подтверждённый профиль обзорщика этого аккаунта.
    public Guid? ReviewerId { get; set; }
    public DateTime CreatedAt { get; set; }
}
