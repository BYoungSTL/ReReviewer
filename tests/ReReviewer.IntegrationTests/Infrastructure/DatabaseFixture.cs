using Microsoft.EntityFrameworkCore;
using ReReviewer.Data;

namespace ReReviewer.IntegrationTests.Infrastructure;

// Отдельная база SQL Server на прогон: создаётся миграциями, удаляется в конце.
// По умолчанию — локальный сервер с входом Windows. Другой задаётся переменной REREVIEWER_TEST_SQL
// (строка подключения без Database, например "Server=localhost,14333;User Id=sa;Password=...;TrustServerCertificate=true").
public sealed class DatabaseFixture : IAsyncLifetime
{
    private const string DefaultServer = "Server=localhost;Trusted_Connection=True;TrustServerCertificate=true";

    public string ConnectionString { get; } =
        $"{Environment.GetEnvironmentVariable("REREVIEWER_TEST_SQL") ?? DefaultServer};Database=ReReviewer_Tests_{Guid.NewGuid():N}";

    // Как в Startup: с ленивой загрузкой навигаций.
    public ReReviewerDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ReReviewerDbContext>()
            .UseLazyLoadingProxies()
            .UseSqlServer(ConnectionString)
            .Options);

    public async ValueTask InitializeAsync()
    {
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await using var db = CreateContext();
        await db.Database.EnsureDeletedAsync();
    }
}

[CollectionDefinition(Name)]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    public const string Name = "Database";
}