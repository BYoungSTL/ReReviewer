using ReReviewer.Data.Entities;
using ReReviewer.Data.Interfaces.Base;
using ReReviewer.Data.Models;

namespace ReReviewer.Data.Interfaces;

public interface IBadgeNominationService : IBaseService<BadgeNomination, Guid>
{
    public Task<BadgeNomination> NominateAsync(Guid userId, Guid reviewerId, int badgeId, CancellationToken cancellationToken = default);
    public Task WithdrawAsync(Guid userId, Guid reviewerId, int badgeId, CancellationToken cancellationToken = default);

    // Знаки, на которые пользователь номинировал этого обзорщика.
    public Task<IReadOnlyList<int>> GetUserBadgeIdsAsync(Guid userId, Guid reviewerId, CancellationToken cancellationToken = default);

    // Очередь модерации: пары «обзорщик + знак» с числом номинаций.
    public Task<IReadOnlyList<BadgeNominationSummary>> GetQueueAsync(int skip = 0, int take = 20, CancellationToken cancellationToken = default);
    public Task<int> CountQueueAsync(CancellationToken cancellationToken = default);

    // Выдаёт обзорщику знак; номинации пары удаляются.
    public Task ApproveAsync(Guid reviewerId, int badgeId, CancellationToken cancellationToken = default);

    // Отклоняет номинации пары: они удаляются, и номинировать можно снова.
    public Task RejectAsync(Guid reviewerId, int badgeId, CancellationToken cancellationToken = default);
}
