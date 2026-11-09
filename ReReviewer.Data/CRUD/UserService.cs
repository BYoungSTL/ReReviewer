using Microsoft.EntityFrameworkCore;
using ReReviewer.Data.CRUD.Base;
using ReReviewer.Data.Entities;
using ReReviewer.Data.Interfaces;

namespace ReReviewer.Data.CRUD;

public class UserService(ReReviewerDbContext context) : BaseService<User, Guid>(context), IUserService
{
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await Set.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    // iat в токене — с точностью до секунды и округлён вниз, поэтому токен, выданный в ту же секунду сразу
    // после смены пароля, иначе тоже был бы отклонён.
    public async Task<bool> AcceptsTokenAsync(Guid userId, DateTime issuedAt, CancellationToken cancellationToken = default)
    {
        var issuedBefore = issuedAt.AddSeconds(1);

        return await Set.AnyAsync(
            u => u.Id == userId && u.BlockedAt == null && (u.PasswordChangedAt == null || u.PasswordChangedAt < issuedBefore),
            cancellationToken);
    }

    public async Task<UserAvatar?> GetAvatarAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await Context.UserAvatars.AsNoTracking().FirstOrDefaultAsync(a => a.Id == userId, cancellationToken);

    public async Task<User> SetAvatarAsync(Guid userId, byte[] content, string contentType, CancellationToken cancellationToken = default)
    {
        var user = await GetRequiredAsync(userId, cancellationToken);
        var avatar = await Context.UserAvatars.FindAsync([userId], cancellationToken);
        var now = DateTime.UtcNow;

        if (avatar is null)
            Context.UserAvatars.Add(new UserAvatar { Id = userId, Content = content, ContentType = contentType, UpdatedAt = now });
        else
        {
            avatar.Content = content;
            avatar.ContentType = contentType;
            avatar.UpdatedAt = now;
        }

        user.AvatarUpdatedAt = now;
        await Context.SaveChangesAsync(cancellationToken);

        return user;
    }

    public async Task<User> RemoveAvatarAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await GetRequiredAsync(userId, cancellationToken);
        var avatar = await Context.UserAvatars.FindAsync([userId], cancellationToken);

        if (avatar is not null)
            Context.UserAvatars.Remove(avatar);

        user.AvatarUpdatedAt = null;
        await Context.SaveChangesAsync(cancellationToken);

        return user;
    }
}
