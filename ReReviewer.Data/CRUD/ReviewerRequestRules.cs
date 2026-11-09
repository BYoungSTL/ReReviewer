using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using ReReviewer.Data.Entities.Enums;

namespace ReReviewer.Data.CRUD;

// Правила, общие для заявки на нового обзорщика и запроса на привязку профиля.
internal static class ReviewerRequestRules
{
    public const int MaxPageSize = 100;

    // Заявить себя обзорщиком (запросом привязки или заявкой «это я») можно, только если аккаунт ещё не привязан
    // к профилю и другой такой заявки на рассмотрении нет: иначе модераторы могли бы одобрить две сразу.
    public static async Task EnsureUserCanClaimAsync(ReReviewerDbContext context, Guid userId, CancellationToken cancellationToken)
    {
        if (await context.Reviewers.AnyAsync(r => r.UserId == userId, cancellationToken))
            throw new ValidationException("Your account is already linked to a reviewer profile.");

        var hasPending =
            await context.ReviewerClaims.AnyAsync(c => c.UserId == userId && c.Status == RequestStatus.Pending, cancellationToken)
            || await context.ReviewerSuggestions.AnyAsync(s => s.UserId == userId && s.IsSelf && s.Status == RequestStatus.Pending, cancellationToken);
        if (hasPending)
            throw new ValidationException("You already have a pending request to link a reviewer profile.");
    }

    // Сравнение без учёта регистра даёт collation базы.
    public static async Task EnsureReviewerNameIsFreeAsync(ReReviewerDbContext context, string fullName, CancellationToken cancellationToken)
    {
        if (await context.Reviewers.AnyAsync(r => r.FullName == fullName, cancellationToken))
            throw new ValidationException("A reviewer with this name already exists.");
    }

    public static void EnsurePending(RequestStatus status)
    {
        if (status != RequestStatus.Pending)
            throw new ValidationException("This request has already been decided.");
    }
}
