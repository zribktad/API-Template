using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Identity.Tests;

/// <summary>
/// Minimal DbContext for handler tests. Uses SQLite in-memory so ExecuteUpdateAsync works.
/// Ignores navigation properties and complex configurations not needed for handler tests.
/// </summary>
internal sealed class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options)
        : base(options) { }

    public DbSet<AppUser> Users => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.OwnsOne(u => u.Audit);
            entity.Ignore(u => u.Tenant);
        });
    }
}
