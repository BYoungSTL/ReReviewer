using ReReviewer.Application.Interfaces;
using ReReviewer.Application.Services.Base;
using ReReviewer.Application.ViewModels;
using ReReviewer.Data.Entities;
using ReReviewer.Data.Interfaces;
using ReReviewer.Data.Models;

namespace ReReviewer.Application.Services;

// Жанры, издания и бейджи обзорщика подгружаются лениво при сборке ViewModel, средняя оценка — из IReviewService.
// Материалы обзорщика — в IMaterialAppService, отзывы о нём — в IReviewAppService.
public class ReviewerAppService(IReviewerService reviewerService, IGenreService genreService, IReviewService reviewService)
    : IReviewerAppService
{
    public async Task<List<ReviewerViewModel>> GetReviewersAsync()
    {
        var reviewers = await reviewerService.GetAllAsync();
        var summaries = await reviewService.GetPublishedSummariesAsync();

        return reviewers
            .OrderBy(r => r.FullName)
            .Select(r => ToViewModel(r, summaries.GetValueOrDefault(r.Id)))
            .ToList();
    }

    public async Task<ReviewerViewModel> GetReviewerAsync(Guid reviewerId) =>
        await ToViewModelAsync(await reviewerService.GetRequiredAsync(reviewerId));

    // Привязка к аккаунту здесь не меняется — для этого ValidateReviewerAsync.
    public async Task<bool> SaveReviewerAsync(ReviewerViewModel reviewer)
    {
        var fullName = InputValidation.RequiredText(reviewer.FullName, "Reviewer name");
        var genreIds = InputValidation.RequiredIds(reviewer.Genres, g => g.Id, "Genre");
        var genres = await genreService.GetByIdsAsync(genreIds);

        var saved = await reviewerService.SaveAsync(reviewer.Id, r =>
        {
            r.FullName = fullName;
            r.Genres.SyncWith(genres);
        });

        reviewer.Id = saved.Id;
        reviewer.FullName = saved.FullName;
        reviewer.UserId = saved.UserId;

        return true;
    }

    // Обзорщика, у которого есть отзывы или материалы, удалить нельзя (ограничение базы).
    public async Task DeleteReviewerAsync(Guid reviewerId) =>
        await reviewerService.DeleteRequiredAsync(reviewerId);

    // Подтверждает, что пользователь — этот обзорщик: привязывает профиль к его аккаунту.
    public async Task<ReviewerViewModel> ValidateReviewerAsync(Guid reviewerId, Guid userId) =>
        await ToViewModelAsync(await reviewerService.ValidateReviewerAsync(new Reviewer { Id = reviewerId }, userId));

    private async Task<ReviewerViewModel> ToViewModelAsync(Reviewer reviewer)
    {
        var summaries = await reviewService.GetPublishedSummariesAsync([reviewer.Id]);

        return ToViewModel(reviewer, summaries.GetValueOrDefault(reviewer.Id));
    }

    private static ReviewerViewModel ToViewModel(Reviewer reviewer, ReviewerScoreSummary? summary) =>
        new()
        {
            Id = reviewer.Id,
            FullName = reviewer.FullName,
            Genres = reviewer.Genres
                .OrderBy(g => g.Title)
                .Select(g => new GenreViewModel { Id = g.Id, Title = g.Title })
                .ToList(),
            UserId = reviewer.UserId,
            Outlets = reviewer.Outlets
                .OrderBy(link => link.Outlet.Name)
                .Select(link => new ReviewerOutletViewModel
                {
                    ReviewerId = link.ReviewerId,
                    ReviewerName = reviewer.FullName,
                    OutletId = link.OutletId,
                    OutletName = link.Outlet.Name,
                    StartedAt = link.StartedAt,
                    EndedAt = link.EndedAt
                })
                .ToList(),
            Badges = reviewer.Badges
                .OrderBy(award => award.AwardedAt)
                .Select(award => new ReviewerBadgeViewModel
                {
                    BadgeId = award.BadgeId,
                    BadgeTitle = award.Badge.Title,
                    AwardedAt = award.AwardedAt
                })
                .ToList(),
            ReviewCount = summary?.ReviewCount ?? 0,
            AverageScore = summary?.AverageScore
        };
}