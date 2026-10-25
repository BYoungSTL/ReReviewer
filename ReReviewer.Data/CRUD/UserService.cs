using Microsoft.EntityFrameworkCore;
using ReReviewer.Data.CRUD.Base;
using ReReviewer.Data.Entities;
using ReReviewer.Data.Interfaces;

namespace ReReviewer.Data.CRUD;

public class UserService(ReReviewerDbContext context) : BaseService<User, Guid>(context), IUserService
{
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await Set.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
}