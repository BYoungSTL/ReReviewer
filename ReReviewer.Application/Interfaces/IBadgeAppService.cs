using ReReviewer.Application.ViewModels;

namespace ReReviewer.Application.Interfaces;

public interface IBadgeAppService
{
    public Task<List<BadgeViewModel>> GetBadgesAsync();
    public Task<BadgeViewModel> GetBadgeAsync(int badgeId);
    public Task<bool> SaveBadgeAsync(BadgeViewModel badge);
    public Task DeleteBadgeAsync(int badgeId);
}