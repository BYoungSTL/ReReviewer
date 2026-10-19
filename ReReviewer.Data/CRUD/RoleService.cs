using ReReviewer.Data.CRUD.Base;
using ReReviewer.Data.Entities;
using ReReviewer.Data.Interfaces;

namespace ReReviewer.Data.CRUD;

public class RoleService(ReReviewerDbContext context) : BaseService<Role, int>(context), IRoleService
{
    
}