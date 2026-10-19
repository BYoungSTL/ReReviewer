using ReReviewer.Data.CRUD.Base;
using ReReviewer.Data.Entities;
using ReReviewer.Data.Interfaces;

namespace ReReviewer.Data.CRUD;

public class GenreService(ReReviewerDbContext context) : BaseService<Genre, int>(context), IGenreService
{
    
}