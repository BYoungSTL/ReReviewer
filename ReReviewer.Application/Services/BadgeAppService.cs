using ReReviewer.Application.Interfaces;
using ReReviewer.Application.Services.Base;
using ReReviewer.Application.ViewModels;
using ReReviewer.Data.Entities;
using ReReviewer.Data.Interfaces;

namespace ReReviewer.Application.Services;

public class BadgeAppService(IBadgeService badgeService) : IBadgeAppService
{
    public async Task<List<BadgeViewModel>> GetBadgesAsync()
    {
        var badges = await badgeService.GetAllAsync();

        return badges.OrderBy(b => b.Title).Select(ToViewModel).ToList();
    }

    public async Task<BadgeViewModel> GetBadgeAsync(int badgeId) =>
        ToViewModel(await badgeService.GetRequiredAsync(badgeId));

    public async Task<bool> SaveBadgeAsync(BadgeViewModel badge)
    {
        var title = InputValidation.RequiredText(badge.Title, "Badge title");

        var saved = await badgeService.SaveAsync(badge.Id, b => b.Title = title);

        badge.Id = saved.Id;
        badge.Title = saved.Title;

        return true;
    }

    // Вместе с бейджем удаляются и его выдачи обзорщикам.
    public async Task DeleteBadgeAsync(int badgeId) =>
        await badgeService.DeleteRequiredAsync(badgeId);

    private static BadgeViewModel ToViewModel(Badge badge) =>
        new()
        {
            Id = badge.Id,
            Title = badge.Title
        };
}