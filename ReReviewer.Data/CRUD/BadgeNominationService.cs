using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using ReReviewer.Data.CRUD.Base;
using ReReviewer.Data.Entities;
using ReReviewer.Data.Interfaces;
using ReReviewer.Data.Models;

namespace ReReviewer.Data.CRUD;

// Номинации обзорщиков на знаки. Модератор решает по паре «обзорщик + знак» целиком: выдаёт знак или отклоняет,
// и в обоих случаях номинации пары удаляются. Отклонённую пару пользователи могут номинировать снова.
public class BadgeNominationService(ReReviewerDbContext context)
    : BaseService<BadgeNomination, Guid>(context), IBadgeNominationService
{
    private const int MaxPageSize = 100;

    public async Task<BadgeNomination> NominateAsync(Guid userId, Guid reviewerId, int badgeId, CancellationToken cancellationToken = default)
    {
        var reviewer = await Context.Reviewers
            .Where(r => r.Id == reviewerId)
            .Select(r => new { r.UserId })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Reviewer not found.");
        if (!await Context.Badges.AnyAsync(b => b.Id == badgeId, cancellationToken))
            throw new KeyNotFoundException("Badge not found.");

        // Как и отзыв о себе: обзорщик с подтверждённым аккаунтом не номинирует сам себя.
        if (reviewer.UserId == userId)
            throw new ValidationException("You cannot nominate your own reviewer profile.");
        if (await Context.ReviewerBadges.AnyAsync(a => a.ReviewerId == reviewerId && a.BadgeId == badgeId, cancellationToken))
            throw new ValidationException("This reviewer already has this badge.");
        if (await Of(reviewerId, badgeId).AnyAsync(n => n.UserId == userId, cancellationToken))
            throw new ValidationException("You have already nominated this reviewer for this badge.");

        // Прокси, чтобы у возвращённой номинации лениво подгружались обзорщик и знак.
        var nomination = Set.CreateProxy(n =>
        {
            n.UserId = userId;
            n.ReviewerId = reviewerId;
            n.BadgeId = badgeId;
        });

        return await AddAsync(nomination, cancellationToken);
    }

    public async Task WithdrawAsync(Guid userId, Guid reviewerId, int badgeId, CancellationToken cancellationToken = default)
    {
        var nomination = await Of(reviewerId, badgeId).FirstOrDefaultAsync(n => n.UserId == userId, cancellationToken)
            ?? throw new KeyNotFoundException("Nomination not found.");

        Set.Remove(nomination);
        await Context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<int>> GetUserBadgeIdsAsync(Guid userId, Guid reviewerId, CancellationToken cancellationToken = default) =>
        await Set
            .Where(n => n.UserId == userId && n.ReviewerId == reviewerId)
            .Select(n => n.BadgeId)
            .ToListAsync(cancellationToken);

    // Первой идёт пара с наибольшим числом номинаций, при равенстве — та, что ждёт дольше.
    public async Task<IReadOnlyList<BadgeNominationSummary>> GetQueueAsync(int skip = 0, int take = 20, CancellationToken cancellationToken = default)
    {
        var rows = await Set
            .GroupBy(n => new { n.ReviewerId, n.Reviewer.FullName, n.BadgeId, n.Badge.Title })
            .Select(g => new
            {
                g.Key.ReviewerId,
                g.Key.FullName,
                g.Key.BadgeId,
                g.Key.Title,
                Count = g.Count(),
                First = g.Min(n => n.CreatedAt),
                Last = g.Max(n => n.CreatedAt)
            })
            .OrderByDescending(g => g.Count).ThenBy(g => g.First).ThenBy(g => g.ReviewerId).ThenBy(g => g.BadgeId)
            .Skip(Math.Max(skip, 0))
            .Take(Math.Clamp(take, 1, MaxPageSize))
            .ToListAsync(cancellationToken);

        // Даты в базе — UTC; после агрегата Min/Max помечаем это явно, как делает UtcDateTimeConverter для столбцов.
        return rows
            .Select(r => new BadgeNominationSummary(r.ReviewerId, r.FullName, r.BadgeId, r.Title, r.Count,
                DateTime.SpecifyKind(r.First, DateTimeKind.Utc), DateTime.SpecifyKind(r.Last, DateTimeKind.Utc)))
            .ToList();
    }

    public async Task<int> CountQueueAsync(CancellationToken cancellationToken = default) =>
        await Set
            .Select(n => new { n.ReviewerId, n.BadgeId })
            .Distinct()
            .CountAsync(cancellationToken);

    // Знак выдаётся и номинации удаляются одним сохранением. Если знак у обзорщика уже есть
    // (например, выдан, пока номинации ждали), номинации просто закрываются.
    public async Task ApproveAsync(Guid reviewerId, int badgeId, CancellationToken cancellationToken = default)
    {
        var nominations = await RequiredPairAsync(reviewerId, badgeId, cancellationToken);

        if (!await Context.ReviewerBadges.AnyAsync(a => a.ReviewerId == reviewerId && a.BadgeId == badgeId, cancellationToken))
            Context.ReviewerBadges.Add(new ReviewerBadge { ReviewerId = reviewerId, BadgeId = badgeId, AwardedAt = DateTime.UtcNow });

        Set.RemoveRange(nominations);
        await Context.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectAsync(Guid reviewerId, int badgeId, CancellationToken cancellationToken = default)
    {
        Set.RemoveRange(await RequiredPairAsync(reviewerId, badgeId, cancellationToken));
        await Context.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<BadgeNomination> Of(Guid reviewerId, int badgeId) =>
        Set.Where(n => n.ReviewerId == reviewerId && n.BadgeId == badgeId);

    private async Task<List<BadgeNomination>> RequiredPairAsync(Guid reviewerId, int badgeId, CancellationToken cancellationToken)
    {
        var nominations = await Of(reviewerId, badgeId).ToListAsync(cancellationToken);

        return nominations.Count > 0
            ? nominations
            : throw new KeyNotFoundException("No nominations for this reviewer and badge.");
    }
}
