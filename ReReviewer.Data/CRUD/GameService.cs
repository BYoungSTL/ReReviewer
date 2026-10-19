using ReReviewer.Data.CRUD.Base;
using ReReviewer.Data.Entities;
using ReReviewer.Data.Interfaces;

namespace ReReviewer.Data.CRUD;

public class GameService(ReReviewerDbContext context) : BaseService<Game, Guid>(context), IGameService
{
    
}