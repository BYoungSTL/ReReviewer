using ReReviewer.Data.CRUD.Base;
using ReReviewer.Data.Entities;
using ReReviewer.Data.Interfaces;

namespace ReReviewer.Data.CRUD;

public class CriterionService(ReReviewerDbContext context) : BaseService<Criterion, int>(context), ICriterionService
{
    
}