using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using ReReviewer.Data.CRUD.Base;
using ReReviewer.Data.Entities;
using ReReviewer.Data.Interfaces;

namespace ReReviewer.Data.CRUD;

public class ReviewerService(ReReviewerDbContext context) : BaseService<Reviewer, Guid>(context), IReviewerService
{
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
}