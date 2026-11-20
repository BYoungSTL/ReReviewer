using ReReviewer.Application.ViewModels;

namespace ReReviewer.Application.Interfaces;

public interface IBadgeNominationAppService
{
    public Task NominateAsync(Guid userId, BadgeNominationViewModel nomination);
    public Task WithdrawAsync(Guid userId, Guid reviewerId, int badgeId);

    // Знаки, на которые пользователь номинировал этого обзорщика.
    public Task<List<int>> GetUserNominationsAsync(Guid userId, Guid reviewerId);

    public Task<PagedViewModel<BadgeNominationSummaryViewModel>> GetModerationQueueAsync(int page = 1, int pageSize = 20);

    // approve — выдать знак, иначе отклонить номинации. В обоих случаях номинации пары удаляются.
    public Task ModerateAsync(Guid reviewerId, int badgeId, bool approve);
}
