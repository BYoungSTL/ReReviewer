namespace ReReviewer.Application.ViewModels;

// Свой профиль обзорщика, который правит владелец подтверждённого аккаунта. Знаки и материалы здесь не меняются.
public class ReviewerProfileViewModel
{
    public string FullName { get; set; } = string.Empty;
    public string? About { get; set; }

    // Адреса http или https, не больше пяти. Пустые строки отбрасываются.
    public List<string> Links { get; set; } = [];

    // Используется только Id, нужен хотя бы один жанр.
    public List<GenreViewModel> Genres { get; set; } = [];

    // Список целиком: чего в нём нет, удаляется. Используются OutletId, StartedAt и EndedAt.
    public List<ReviewerOutletViewModel> Outlets { get; set; } = [];
}
