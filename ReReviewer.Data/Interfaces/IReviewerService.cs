using ReReviewer.Data.Entities;
using ReReviewer.Data.Interfaces.Base;
using ReReviewer.Data.Models;

namespace ReReviewer.Data.Interfaces;

public interface IReviewerService : IBaseService<Reviewer, Guid>
{
    public Task<Reviewer> ValidateReviewerAsync(Reviewer reviewer, Guid userId);
    public Task<Reviewer> LinkMaterialsAsync(Reviewer reviewer, List<Material> materials);

    // Профиль, привязанный к аккаунту пользователя. Знаки и материалы здесь не меняются.
    public Task<Reviewer> UpdateOwnProfileAsync(Guid userId, ReviewerProfileInput input, CancellationToken cancellationToken = default);
}