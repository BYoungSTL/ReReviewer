using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ReReviewer.Data.Entities;
using ReReviewer.Data.Entities.Enums;
using ReReviewer.IntegrationTests.Infrastructure;

namespace ReReviewer.IntegrationTests.Data;

// Миграции RequireGeneralCriteriaAsPercent и RemoveOptionalCriteria на данных: своя база, сначала схема до них,
// потом обе миграции.
public sealed class CriteriaMigrationTests : IAsyncLifetime
{
    private const string BeforePercent = "20261109162354_AddReviewerAboutAndLinks";

    // Только ради строки подключения и удаления базы: миграции здесь применяются вручную, а не в InitializeAsync фикстуры.
    private readonly DatabaseFixture _database = new();

    public ValueTask InitializeAsync() =>
        ValueTask.CompletedTask;

    public async ValueTask DisposeAsync() =>
        await _database.DisposeAsync();

    [Fact]
    public async Task Migrations_ConvertScoresToPercent_FillMissingCriteria_AndRemoveGenreCriteria()
    {
        Guid reviewId;
        int scored, missing;
        await using (var db = _database.CreateContext())
        {
            await db.GetService<IMigrator>().MigrateAsync(BeforePercent);

            var user = new User { Name = "user", Email = "migration@test.local", PasswordHash = "x" };
            var reviewer = new Reviewer { FullName = "Reviewer" };
            var general1 = new Criterion { Title = "Scored general" };
            var general2 = new Criterion { Title = "Missing general" };
            var review = new Review { User = user, Reviewer = reviewer, Text = "Text", Score = 7, Status = ReviewStatus.Published };
            review.CriterionScores.Add(new ReviewCriterionScore { Criterion = general1, Score = 9 });
            db.AddRange(user, reviewer, general1, general2, review);
            await db.SaveChangesAsync();

            // У сущности жанра у критерия уже нет, поэтому жанровый критерий и его оценка — прямым SQL по старой схеме.
            await db.Database.ExecuteSqlAsync($"""
                INSERT INTO Genres (Title) VALUES (N'Genre');
                INSERT INTO Criteria (Title, GenreId) VALUES (N'Genre criterion', SCOPE_IDENTITY());
                INSERT INTO ReviewCriterionScores (ReviewId, CriterionId, Score) VALUES ({review.Id}, SCOPE_IDENTITY(), 8);
                """);

            (reviewId, scored, missing) = (review.Id, general1.Id, general2.Id);
        }

        await using (var db = _database.CreateContext())
            await db.Database.MigrateAsync();

        await using var check = _database.CreateContext();
        var scores = await check.ReviewCriterionScores
            .Where(s => s.ReviewId == reviewId)
            .ToDictionaryAsync(s => s.CriterionId, s => s.Score);

        scores.ShouldBe(new Dictionary<int, byte> { [scored] = 90, [missing] = 70 }, ignoreOrder: true);
        (await check.Criteria.Select(c => c.Title).ToListAsync()).ShouldBe(["Scored general", "Missing general"], ignoreOrder: true);
    }
}
