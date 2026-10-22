namespace ReReviewer.Application.ViewModels;

public class ReviewerViewModel
{
    // Null — новый обзорщик, иначе — правка существующего.
    public Guid? Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    // При сохранении используется только Id жанров.
    public List<GenreViewModel> Genres { get; set; } = [];

    #region Только чтение

    // Аккаунт, к которому привязан профиль (IReviewerAppService.ValidateReviewerAsync).
    public Guid? UserId { get; set; }
    public List<ReviewerOutletViewModel> Outlets { get; set; } = [];
    public List<ReviewerBadgeViewModel> Badges { get; set; } = [];

    #endregion
}