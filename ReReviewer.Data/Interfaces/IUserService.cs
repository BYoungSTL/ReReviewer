using ReReviewer.Data.Entities;
using ReReviewer.Data.Interfaces.Base;

namespace ReReviewer.Data.Interfaces;

public interface IUserService : IBaseService<User, Guid>
{
    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    // Пользователь существует и не заблокирован.
    public Task<bool> IsActiveAsync(Guid userId, CancellationToken cancellationToken = default);
}