using ReReviewer.Data.CRUD.Base;
using ReReviewer.Data.Entities;
using ReReviewer.Data.Interfaces;

namespace ReReviewer.Data.CRUD;

public class BadgeService(ReReviewerDbContext context) : BaseService<Badge, int>(context), IBadgeService
{
    
}