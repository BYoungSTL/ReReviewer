using System.ComponentModel.DataAnnotations;
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
    private const int MaxNameLength = 250;
    private const int MaxAboutLength = 1000;
    private const int MaxLinks = 5;
    private const int MaxLinkLength = 300;

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

    // Здесь — проверка текста и ссылок. Чей это профиль, свободно ли имя, существуют ли жанры и издания — в слое Data.
    public async Task<ReviewerViewModel> UpdateOwnProfileAsync(Guid userId, ReviewerProfileViewModel profile)
    {
        var fullName = MaxLength(InputValidation.RequiredText(profile.FullName, "Reviewer name"), MaxNameLength, "Reviewer name");
        // Браузер отправляет переводы строк из textarea как \r\n, а maxlength и счётчик на сайте считают их одним символом.
        var about = InputValidation.OptionalText(profile.About)?.ReplaceLineEndings("\n");
        if (about is not null)
            MaxLength(about, MaxAboutLength, "About");

        var input = new ReviewerProfileInput(
            fullName,
            about,
            NormalizeLinks(profile.Links),
            InputValidation.RequiredIds(profile.Genres, g => g.Id, "Genre"),
            profile.Outlets.Select(o => new ReviewerOutletInput(o.OutletId, o.StartedAt, o.EndedAt)).ToList());

        return await ToViewModelAsync(await reviewerService.UpdateOwnProfileAsync(userId, input));
    }

    // Только абсолютные адреса http и https: ссылки выводятся на странице обзорщика, javascript: и прочее туда не попадёт.
    private static List<string> NormalizeLinks(IEnumerable<string?> links)
    {
        var result = links
            .Select(InputValidation.OptionalText)
            .OfType<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (result.Count > MaxLinks)
            throw new ValidationException($"At most {MaxLinks} links are allowed.");

        foreach (var link in result)
        {
            MaxLength(link, MaxLinkLength, "Link");
            if (!Uri.TryCreate(link, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                throw new ValidationException("Links must be http or https addresses.");
        }

        return result;
    }

    private static string MaxLength(string value, int maxLength, string fieldName) =>
        value.Length > maxLength
            ? throw new ValidationException($"{fieldName} must be at most {maxLength} characters long.")
            : value;

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
            AverageScore = summary?.AverageScore,
            About = reviewer.About,
            Links = [.. reviewer.Links]
        };
}