using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReReviewer.Data.Entities;

namespace ReReviewer.Data;

public class ReReviewerDbContext : DbContext
{
    public ReReviewerDbContext(DbContextOptions<ReReviewerDbContext> options) : base(options)
    {

    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Reviewer> Reviewers => Set<Reviewer>();
    public DbSet<Outlet> Outlets => Set<Outlet>();
    public DbSet<ReviewerOutlet> ReviewerOutlets => Set<ReviewerOutlet>();
    public DbSet<Badge> Badges => Set<Badge>();
    public DbSet<ReviewerBadge> ReviewerBadges => Set<ReviewerBadge>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Criterion> Criteria => Set<Criterion>();
    public DbSet<ReviewCriterionScore> ReviewCriterionScores => Set<ReviewCriterionScore>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        UserConfiguration(modelBuilder.Entity<User>());
        RoleConfiguration(modelBuilder.Entity<Role>());
        ReviewerConfiguration(modelBuilder.Entity<Reviewer>());
        OutletConfiguration(modelBuilder.Entity<Outlet>());
        ReviewerOutletConfiguration(modelBuilder.Entity<ReviewerOutlet>());
        BadgeConfiguration(modelBuilder.Entity<Badge>());
        ReviewerBadgeConfiguration(modelBuilder.Entity<ReviewerBadge>());
        GenreConfiguration(modelBuilder.Entity<Genre>());
        GameConfiguration(modelBuilder.Entity<Game>());
        MaterialConfiguration(modelBuilder.Entity<Material>());
        ReviewConfiguration(modelBuilder.Entity<Review>());
        CriterionConfiguration(modelBuilder.Entity<Criterion>());
        ReviewCriterionScoreConfiguration(modelBuilder.Entity<ReviewCriterionScore>());
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {

    }

    // Каскадом удаляется только то, что принадлежит удаляемой записи:
    // отзывы пользователя, баллы отзыва и связи many-to-many (в том числе ссылки отзыва на материалы).
    // Обзорщика, игру, жанр и критерий нельзя удалить, пока на них ссылаются отзывы, материалы
    // или оценки, — чтобы случайно не снести отзывы пользователей.
    #region EntitiesConfigs

    private void UserConfiguration(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).IsRequired();
        builder.Property(e => e.Name).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Email).IsRequired().HasMaxLength(250);
        builder.Property(e => e.PasswordHash).IsRequired();
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(e => e.Email).IsUnique();

        builder
            .HasMany(e => e.Roles)
            .WithMany(e => e.Users)
            .UsingEntity(j => j.ToTable("UserRoles"));

        builder
            .HasMany(e => e.Reviews)
            .WithOne(e => e.User)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(e => e.Reviewer)
            .WithOne(e => e.User)
            .HasForeignKey<Reviewer>(e => e.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    private void RoleConfiguration(EntityTypeBuilder<Role> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).IsRequired();
        builder.Property(e => e.Title).IsRequired().HasMaxLength(100);

        builder.HasIndex(e => e.Title).IsUnique();
    }

    private void ReviewerConfiguration(EntityTypeBuilder<Reviewer> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).IsRequired();
        builder.Property(e => e.FullName).IsRequired().HasMaxLength(250);

        builder
            .HasMany(e => e.Materials)
            .WithOne(e => e.Reviewer)
            .HasForeignKey(e => e.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasMany(e => e.Reviews)
            .WithOne(e => e.Reviewer)
            .HasForeignKey(e => e.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasMany(e => e.Outlets)
            .WithOne(e => e.Reviewer)
            .HasForeignKey(e => e.ReviewerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(e => e.Badges)
            .WithOne(e => e.Reviewer)
            .HasForeignKey(e => e.ReviewerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(e => e.Genres)
            .WithMany(e => e.Reviewers)
            .UsingEntity(j => j.ToTable("ReviewerGenres"));
    }

    private void OutletConfiguration(EntityTypeBuilder<Outlet> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).IsRequired();
        builder.Property(e => e.Name).IsRequired().HasMaxLength(100);

        builder.HasIndex(e => e.Name).IsUnique();

        builder
            .HasMany(e => e.Reviewers)
            .WithOne(e => e.Outlet)
            .HasForeignKey(e => e.OutletId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(e => e.Materials)
            .WithOne(e => e.Outlet)
            .HasForeignKey(e => e.OutletId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    private void ReviewerOutletConfiguration(EntityTypeBuilder<ReviewerOutlet> builder) =>
        builder.HasKey(e => new { e.ReviewerId, e.OutletId });

    private void BadgeConfiguration(EntityTypeBuilder<Badge> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).IsRequired();
        builder.Property(e => e.Title).IsRequired().HasMaxLength(250);

        builder.HasIndex(e => e.Title).IsUnique();

        builder
            .HasMany(e => e.Reviewers)
            .WithOne(e => e.Badge)
            .HasForeignKey(e => e.BadgeId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private void ReviewerBadgeConfiguration(EntityTypeBuilder<ReviewerBadge> builder)
    {
        builder.HasKey(e => new { e.ReviewerId, e.BadgeId });

        builder.Property(e => e.AwardedAt).HasDefaultValueSql("SYSUTCDATETIME()");
    }

    private void GenreConfiguration(EntityTypeBuilder<Genre> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).IsRequired();
        builder.Property(e => e.Title).IsRequired().HasMaxLength(100);

        builder.HasIndex(e => e.Title).IsUnique();

        builder
            .HasMany(e => e.Games)
            .WithMany(e => e.Genres)
            .UsingEntity(j => j.ToTable("GameGenres"));

        builder
            .HasMany(e => e.Criteria)
            .WithOne(e => e.Genre)
            .HasForeignKey(e => e.GenreId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private void GameConfiguration(EntityTypeBuilder<Game> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).IsRequired();
        builder.Property(e => e.Title).IsRequired().HasMaxLength(250);

        builder
            .HasMany(e => e.Materials)
            .WithOne(e => e.Game)
            .HasForeignKey(e => e.GameId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private void MaterialConfiguration(EntityTypeBuilder<Material> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).IsRequired();
        builder.Property(e => e.Title).IsRequired().HasMaxLength(250);
        // 800 символов nvarchar — 1600 байт: укладывается в лимит ключа индекса SQL Server (1700 байт).
        builder.Property(e => e.Url).IsRequired().HasMaxLength(800);
        builder.Property(e => e.Format).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.OriginalScore).HasPrecision(3, 1);
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(e => e.Url).IsUnique();

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Materials_OriginalScore", "[OriginalScore] BETWEEN 0 AND 10"));
    }

    private void ReviewConfiguration(EntityTypeBuilder<Review> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).IsRequired();
        builder.Property(e => e.Text).IsRequired();
        builder.Property(e => e.Score).IsRequired();
        builder.Property(e => e.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        // Один отзыв от пользователя на одного обзорщика; новое мнение — правка существующего отзыва.
        builder.HasIndex(e => new { e.UserId, e.ReviewerId }).IsUnique();

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Reviews_Score", "[Score] BETWEEN 1 AND 10"));

        // Что материалы принадлежат тому же обзорщику, что и отзыв, проверяется в сервисном слое.
        builder
            .HasMany(e => e.Materials)
            .WithMany(e => e.Reviews)
            .UsingEntity(j => j.ToTable("ReviewMaterials"));

        builder
            .HasMany(e => e.CriterionScores)
            .WithOne(e => e.Review)
            .HasForeignKey(e => e.ReviewId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private void CriterionConfiguration(EntityTypeBuilder<Criterion> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).IsRequired();
        builder.Property(e => e.Title).IsRequired().HasMaxLength(250);

        builder
            .HasMany(e => e.Scores)
            .WithOne(e => e.Criterion)
            .HasForeignKey(e => e.CriterionId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private void ReviewCriterionScoreConfiguration(EntityTypeBuilder<ReviewCriterionScore> builder)
    {
        builder.HasKey(e => new { e.ReviewId, e.CriterionId });

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_ReviewCriterionScores_Score", "[Score] BETWEEN 1 AND 10"));
    }
    #endregion
}