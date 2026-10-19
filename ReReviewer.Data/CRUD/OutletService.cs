using Microsoft.EntityFrameworkCore;
using ReReviewer.Data.CRUD.Base;
using ReReviewer.Data.Entities;
using ReReviewer.Data.Interfaces;

namespace ReReviewer.Data.CRUD;

public class OutletService(ReReviewerDbContext context) : BaseService<Outlet, Guid>(context), IOutletService
{
    // Уже привязанные обзорщики пропускаются, поэтому повторный вызов ничего не дублирует.
    // От переданных объектов берётся только Id: они могут прийти из запроса неотслеживаемыми.
    public async Task<Outlet> LinkReviewersAsync(Guid outletId, List<Reviewer> reviewers)
    {
        var outlet = await Set
            .Include(o => o.Reviewers).ThenInclude(link => link.Reviewer)
            .FirstOrDefaultAsync(o => o.Id == outletId)
            ?? throw new KeyNotFoundException("Outlet not found.");

        var reviewerIds = reviewers.Select(r => r.Id).Distinct().ToList();
        var found = await Context.Reviewers
            .Where(r => reviewerIds.Contains(r.Id))
            .ToListAsync();

        if (found.Count != reviewerIds.Count)
            throw new KeyNotFoundException("One or more reviewers not found.");

        foreach (var reviewer in found.Where(r => outlet.Reviewers.All(link => link.ReviewerId != r.Id)))
            outlet.Reviewers.Add(new ReviewerOutlet { Reviewer = reviewer });

        await Context.SaveChangesAsync();

        return outlet;
    }
}