using Microsoft.EntityFrameworkCore;
using MiniHn.Domain;

namespace MiniHn.Infrastructure.Persistence;

/// <summary>The single EF Core context. Phase I owns one table.</summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    /// <summary>Every stored post.</summary>
    public DbSet<Post> Posts => Set<Post>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
