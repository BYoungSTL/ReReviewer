using Microsoft.EntityFrameworkCore;

namespace ReReviewer.Data;

public class ReReviewerDbContext : DbContext
{
    public ReReviewerDbContext(DbContextOptions<ReReviewerDbContext> options) : base(options)
    {
        
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); 
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        
    }
}