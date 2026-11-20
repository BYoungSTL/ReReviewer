using ReReviewer.Application.Interfaces;
using ReReviewer.Application.ViewModels;
using ReReviewer.Data.Interfaces;

namespace ReReviewer.Application.Services;

// Правила номинаций (не себя, не на уже выданный знак, один раз) проверяет IBadgeNominationService: им нужна база.
public class BadgeNominationAppService(IBadgeNominationService nominationService) : IBadgeNominationAppService
{
    private const int MaxPageSize = 100;

    public async Task NominateAsync(Guid userId, BadgeNominationViewModel nomination) =>
        await nominationService.NominateAsync(userId, nomination.ReviewerId, nomination.BadgeId);

    public async Task WithdrawAsync(Guid userId, Guid reviewerId, int badgeId) =>
        await nominationService.WithdrawAsync(userId, reviewerId, badgeId);

    public async Task<List<int>> GetUserNominationsAsync(Guid userId, Guid reviewerId) =>
        [.. await nominationService.GetUserBadgeIdsAsync(userId, reviewerId)];

    // Первой идёт пара с наибольшим числом номинаций, при равенстве — та, что ждёт дольше.
    public async Task<PagedViewModel<BadgeNominationSummaryViewModel>> GetModerationQueueAsync(int page = 1, int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var rows = await nominationService.GetQueueAsync((page - 1) * pageSize, pageSize);
        var totalCount = await nominationService.CountQueueAsync();

        return new()
        {
            Items = rows
                .Select(r => new BadgeNominationSummaryViewModel
                {
                    ReviewerId = r.ReviewerId,
                    ReviewerName = r.ReviewerName,
                    BadgeId = r.BadgeId,
                    BadgeTitle = r.BadgeTitle,
                    NominationCount = r.NominationCount,
                    FirstNominatedAt = r.FirstNominatedAt,
                    LastNominatedAt = r.LastNominatedAt
                })
                .ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task ModerateAsync(Guid reviewerId, int badgeId, bool approve)
    {
        if (approve)
            await nominationService.ApproveAsync(reviewerId, badgeId);
        else
            await nominationService.RejectAsync(reviewerId, badgeId);
    }
}
