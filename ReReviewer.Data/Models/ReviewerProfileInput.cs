namespace ReReviewer.Data.Models;

// Профиль, который обзорщик с подтверждённым аккаунтом правит сам. Текст уже проверен и обрезан в Application.
// Места работы задаются целиком: чего нет в списке, удаляется.
public record ReviewerProfileInput(
    string FullName,
    string? About,
    IReadOnlyList<string> Links,
    IReadOnlyCollection<int> GenreIds,
    IReadOnlyList<ReviewerOutletInput> Outlets);

public record ReviewerOutletInput(
    Guid OutletId,
    DateOnly? StartedAt,
    DateOnly? EndedAt);
