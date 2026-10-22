using ReReviewer.Application.Interfaces;
using ReReviewer.Application.Services.Base;
using ReReviewer.Application.ViewModels;
using ReReviewer.Data.Entities;
using ReReviewer.Data.Interfaces;

namespace ReReviewer.Application.Services;

// Обзорщики издания подгружаются лениво при сборке ViewModel.
public class OutletAppService(IOutletService outletService) : IOutletAppService
{
    public async Task<List<OutletViewModel>> GetOutletsAsync()
    {
        var outlets = await outletService.GetAllAsync();

        return outlets.OrderBy(o => o.Name).Select(ToViewModel).ToList();
    }

    public async Task<OutletViewModel> GetOutletAsync(Guid outletId) =>
        ToViewModel(await outletService.GetRequiredAsync(outletId));

    public async Task<bool> SaveOutletAsync(OutletViewModel outlet)
    {
        var name = InputValidation.RequiredText(outlet.Name, "Outlet name");

        var saved = await outletService.SaveAsync(outlet.Id, o => o.Name = name);

        outlet.Id = saved.Id;
        outlet.Name = saved.Name;

        return true;
    }

    // У материалов этого издания издание просто сбрасывается, сами материалы остаются.
    public async Task DeleteOutletAsync(Guid outletId) =>
        await outletService.DeleteRequiredAsync(outletId);

    // Уже привязанные обзорщики пропускаются.
    public async Task<OutletViewModel> LinkReviewersAsync(Guid outletId, List<Guid> reviewerIds)
    {
        var reviewers = reviewerIds.Select(id => new Reviewer { Id = id }).ToList();

        return ToViewModel(await outletService.LinkReviewersAsync(outletId, reviewers));
    }

    private static OutletViewModel ToViewModel(Outlet outlet) =>
        new()
        {
            Id = outlet.Id,
            Name = outlet.Name,
            Reviewers = outlet.Reviewers
                .OrderBy(link => link.Reviewer.FullName)
                .Select(link => new ReviewerOutletViewModel
                {
                    ReviewerId = link.ReviewerId,
                    ReviewerName = link.Reviewer.FullName,
                    OutletId = link.OutletId,
                    OutletName = outlet.Name,
                    StartedAt = link.StartedAt,
                    EndedAt = link.EndedAt
                })
                .ToList()
        };
}