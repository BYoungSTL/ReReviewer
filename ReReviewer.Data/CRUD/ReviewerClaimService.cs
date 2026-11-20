using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using ReReviewer.Data.CRUD.Base;
using ReReviewer.Data.Entities;
using ReReviewer.Data.Entities.Enums;
using ReReviewer.Data.Interfaces;

namespace ReReviewer.Data.CRUD;

public class ReviewerClaimService(ReReviewerDbContext context)
    : BaseService<ReviewerClaim, Guid>(context), IReviewerClaimService
{
    public async Task<ReviewerClaim> CreateAsync(Guid userId, Guid reviewerId, string note, CancellationToken cancellationToken = default)
    {
        var reviewer = await Context.Reviewers.FindAsync([reviewerId], cancellationToken)
            ?? throw new KeyNotFoundException("Reviewer not found.");
        if (reviewer.UserId is not null)
            throw new ValidationException("This reviewer profile is already linked to a user.");

        await ReviewerRequestRules.EnsureUserCanClaimAsync(Context, userId, cancellationToken);

        // Прокси, чтобы у возвращённого запроса лениво подгружались профиль и автор.
        var claim = Set.CreateProxy(c =>
        {
            c.UserId = userId;
            c.ReviewerId = reviewerId;
            c.Note = note;
            c.Status = RequestStatus.Pending;
        });

        return await AddAsync(claim, cancellationToken);
    }

    public async Task<IReadOnlyList<ReviewerClaim>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await Set
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt).ThenBy(c => c.Id)
            .ToListAsync(cancellationToken);

    // Первым идёт запрос, который ждёт дольше всех.
    public async Task<IReadOnlyList<ReviewerClaim>> GetPendingAsync(int skip = 0, int take = 20, CancellationToken cancellationToken = default) =>
        await Pending()
            .OrderBy(c => c.CreatedAt).ThenBy(c => c.Id)
            .Skip(Math.Max(skip, 0))
            .Take(Math.Clamp(take, 1, ReviewerRequestRules.MaxPageSize))
            .ToListAsync(cancellationToken);

    public async Task<int> CountPendingAsync(CancellationToken cancellationToken = default) =>
        await Pending().CountAsync(cancellationToken);

    // Привязывает профиль к автору запроса. Другие запросы на этот профиль после этого теряют смысл
    // и отклоняются в том же сохранении.
    public async Task<ReviewerClaim> ApproveAsync(Guid claimId, CancellationToken cancellationToken = default)
    {
        var claim = await GetRequiredAsync(claimId, cancellationToken);
        ReviewerRequestRules.EnsurePending(claim.Status);

        if (claim.Reviewer.UserId is not null)
            throw new ValidationException("This reviewer profile is already linked to a user.");
        if (await Context.Reviewers.AnyAsync(r => r.UserId == claim.UserId, cancellationToken))
            throw new ValidationException("This user is already linked to another reviewer profile.");

        var now = DateTime.UtcNow;
        claim.Reviewer.UserId = claim.UserId;
        claim.Status = RequestStatus.Approved;
        claim.DecidedAt = now;

        var competing = await Pending()
            .Where(c => c.ReviewerId == claim.ReviewerId && c.Id != claim.Id)
            .ToListAsync(cancellationToken);
        foreach (var other in competing)
        {
            other.Status = RequestStatus.Rejected;
            other.DecidedAt = now;
        }

        await Context.SaveChangesAsync(cancellationToken);

        return claim;
    }

    public async Task<ReviewerClaim> RejectAsync(Guid claimId, CancellationToken cancellationToken = default)
    {
        var claim = await GetRequiredAsync(claimId, cancellationToken);
        ReviewerRequestRules.EnsurePending(claim.Status);

        claim.Status = RequestStatus.Rejected;
        claim.DecidedAt = DateTime.UtcNow;
        await Context.SaveChangesAsync(cancellationToken);

        return claim;
    }

    private IQueryable<ReviewerClaim> Pending() =>
        Set.Where(c => c.Status == RequestStatus.Pending);
}
