using ReReviewer.Data.Entities;
using ReReviewer.Data.Interfaces.Base;

namespace ReReviewer.Data.Interfaces;

public interface IUserService : IBaseService<User, Guid>
{
    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    // Пользователь существует, не заблокирован и не менял пароль после выдачи токена.
    public Task<bool> AcceptsTokenAsync(Guid userId, DateTime issuedAt, CancellationToken cancellationToken = default);

    public Task<UserAvatar?> GetAvatarAsync(Guid userId, CancellationToken cancellationToken = default);

    // Заменяет аватарку и отмечает время в User.AvatarUpdatedAt — одним сохранением.
    public Task<User> SetAvatarAsync(Guid userId, byte[] content, string contentType, CancellationToken cancellationToken = default);
    public Task<User> RemoveAvatarAsync(Guid userId, CancellationToken cancellationToken = default);
}
