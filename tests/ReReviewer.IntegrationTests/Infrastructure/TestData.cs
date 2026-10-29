using ReReviewer.Data;
using ReReviewer.Data.Entities;
using ReReviewer.Data.Entities.Enums;

namespace ReReviewer.IntegrationTests.Infrastructure;

// Записи для тестов. Названия и почты уникальны, поэтому тесты не мешают друг другу в общей базе.
public static class TestData
{
    public static string Unique(string prefix) =>
        $"{prefix} {Guid.NewGuid():N}";

    public static Task<User> UserAsync(ReReviewerDbContext db) =>
        SaveAsync(db, new User { Name = "user", Email = $"{Guid.NewGuid():N}@test.local", PasswordHash = "x" });

    public static Task<Reviewer> ReviewerAsync(ReReviewerDbContext db, Guid? userId = null) =>
        SaveAsync(db, new Reviewer { FullName = Unique("Reviewer"), UserId = userId });

    public static Task<Game> GameAsync(ReReviewerDbContext db) =>
        SaveAsync(db, new Game { Title = Unique("Game") });

    public static Task<Outlet> OutletAsync(ReReviewerDbContext db) =>
        SaveAsync(db, new Outlet { Name = Unique("Outlet") });

    public static Task<Genre> GenreAsync(ReReviewerDbContext db) =>
        SaveAsync(db, new Genre { Title = Unique("Genre") });

    public static Task<Criterion> CriterionAsync(ReReviewerDbContext db) =>
        SaveAsync(db, new Criterion { Title = Unique("Criterion") });

    public static Task<Material> MaterialAsync(ReReviewerDbContext db, Guid reviewerId, Guid gameId) =>
        SaveAsync(db, new Material
        {
            ReviewerId = reviewerId,
            GameId = gameId,
            Title = Unique("Material"),
            Url = $"https://example.com/{Guid.NewGuid():N}",
            Format = MaterialFormat.Video
        });

    private static async Task<T> SaveAsync<T>(ReReviewerDbContext db, T entity) where T : class
    {
        db.Add(entity);
        await db.SaveChangesAsync();

        return entity;
    }
}