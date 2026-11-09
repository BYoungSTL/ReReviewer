using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using ReReviewer.Data.CRUD.Base;
using ReReviewer.Data.Entities;
using ReReviewer.Data.Interfaces;
using ReReviewer.Data.Models;

namespace ReReviewer.Data.CRUD;

public class ReviewerService(ReReviewerDbContext context) : BaseService<Reviewer, Guid>(context), IReviewerService
{
    // Профиль находится по аккаунту, а не по Id из запроса, поэтому править можно только свой.
    // Имя не должно совпадать с чужим: иначе подтверждённый аккаунт мог бы выдать себя за другого обзорщика.
    public async Task<Reviewer> UpdateOwnProfileAsync(Guid userId, ReviewerProfileInput input, CancellationToken cancellationToken = default)
    {
        var reviewer = await Set.FirstOrDefaultAsync(r => r.UserId == userId, cancellationToken)
            ?? throw new KeyNotFoundException("Your account is not linked to a reviewer profile.");

        if (await Set.AnyAsync(r => r.Id != reviewer.Id && r.FullName == input.FullName, cancellationToken))
            throw new ValidationException("A reviewer with this name already exists.");

        var genreIds = input.GenreIds.Distinct().ToList();
        if (genreIds.Count == 0)
            throw new ValidationException("At least one genre is required.");
        var genres = await Context.Genres.Where(g => genreIds.Contains(g.Id)).ToListAsync(cancellationToken);
        if (genres.Count != genreIds.Count)
            throw new KeyNotFoundException("One or more genres not found.");

        var outletIds = input.Outlets.Select(o => o.OutletId).ToList();
        if (outletIds.Distinct().Count() != outletIds.Count)
            throw new ValidationException("Each outlet can be listed only once.");
        if (input.Outlets.Any(o => o.StartedAt > o.EndedAt))
            throw new ValidationException("An outlet's end date cannot be earlier than its start date.");
        if (await Context.Outlets.CountAsync(o => outletIds.Contains(o.Id), cancellationToken) != outletIds.Count)
            throw new KeyNotFoundException("One or more outlets not found.");

        reviewer.FullName = input.FullName;
        reviewer.About = input.About;
        reviewer.Links = [.. input.Links];

        // Коллекции подгружаются лениво и синхронизируются точечно, чтобы не пересоздавать неизменившиеся строки.
        foreach (var removed in reviewer.Genres.Where(g => !genreIds.Contains(g.Id)).ToList())
            reviewer.Genres.Remove(removed);
        foreach (var added in genres.Where(g => reviewer.Genres.All(x => x.Id != g.Id)).ToList())
            reviewer.Genres.Add(added);

        SyncOutlets(reviewer, input.Outlets);

        await Context.SaveChangesAsync(cancellationToken);

        return reviewer;
    }

    // Привязывает профиль обзорщика к аккаунту пользователя (подтверждение, что это он).
    // Кто имеет право подтверждать, проверяется на уровне контроллера, например только модератор.
    public async Task<Reviewer> ValidateReviewerAsync(Reviewer reviewer, Guid userId)
    {
        var profile = await GetRequiredAsync(reviewer.Id);

        if (profile.UserId == userId)
            return profile;

        if (profile.UserId is not null)
            throw new ValidationException("This reviewer profile is already linked to another user.");

        if (!await Context.Users.AnyAsync(u => u.Id == userId))
            throw new KeyNotFoundException("User not found.");

        if (await Set.AnyAsync(r => r.UserId == userId))
            throw new ValidationException("This user is already linked to another reviewer profile.");

        profile.UserId = userId;
        await Context.SaveChangesAsync();

        return profile;
    }

    // Новые материалы создаются у этого обзорщика, уже принадлежащие ему пропускаются.
    // Материал другого обзорщика не переносится: на него могут ссылаться отзывы о том обзорщике.
    public async Task<Reviewer> LinkMaterialsAsync(Reviewer reviewer, List<Material> materials)
    {
        var profile = await GetRequiredAsync(reviewer.Id);

        var ids = materials.Where(m => m.Id != Guid.Empty).Select(m => m.Id).ToList();
        var existing = await Context.Materials
            .Where(m => ids.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id);

        var foreign = existing.Values.FirstOrDefault(m => m.ReviewerId != profile.Id);
        if (foreign is not null)
            throw new ValidationException($"Material {foreign.Id} belongs to another reviewer.");

        // Новые материалы копируются в прокси, а не добавляются как есть: переданные объекты могут нести
        // неотслеживаемые Game или Outlet, и EF попытался бы вставить их в базу заново.
        // profile.Materials подгружается лениво при первом обращении.
        foreach (var material in materials.Where(m => !existing.ContainsKey(m.Id)))
        {
            profile.Materials.Add(Context.CreateProxy<Material>(m =>
            {
                m.Id = material.Id;
                m.GameId = material.GameId;
                m.OutletId = material.OutletId;
                m.Title = material.Title;
                m.Url = material.Url;
                m.Format = material.Format;
                m.OriginalScore = material.OriginalScore;
                m.PublishedAt = material.PublishedAt;
            }));
        }

        await Context.SaveChangesAsync();

        return profile;
    }

    // Места работы, которых нет в новом списке, удаляются; у оставшихся обновляются даты.
    private void SyncOutlets(Reviewer reviewer, IReadOnlyList<ReviewerOutletInput> outlets)
    {
        foreach (var removed in reviewer.Outlets.Where(link => outlets.All(o => o.OutletId != link.OutletId)).ToList())
            reviewer.Outlets.Remove(removed);

        foreach (var outlet in outlets)
        {
            var existing = reviewer.Outlets.FirstOrDefault(link => link.OutletId == outlet.OutletId);
            if (existing is not null)
            {
                existing.StartedAt = outlet.StartedAt;
                existing.EndedAt = outlet.EndedAt;
                continue;
            }

            reviewer.Outlets.Add(Context.CreateProxy<ReviewerOutlet>(link =>
            {
                link.ReviewerId = reviewer.Id;
                link.OutletId = outlet.OutletId;
                link.StartedAt = outlet.StartedAt;
                link.EndedAt = outlet.EndedAt;
            }));
        }
    }
}