using ReReviewer.Data.CRUD.Base;
using ReReviewer.Data.Entities;
using ReReviewer.Data.Interfaces;

namespace ReReviewer.Data.CRUD;

public class MaterialService(ReReviewerDbContext context) : BaseService<Material, Guid>(context), IMaterialService
{
    
}