using ReReviewer.Data.CRUD.Base;
using ReReviewer.Data.Entities;
using ReReviewer.Data.Interfaces;

namespace ReReviewer.Data.CRUD;

public class UserService(ReReviewerDbContext context) : BaseService<User, Guid>(context), IUserService
{
    
}