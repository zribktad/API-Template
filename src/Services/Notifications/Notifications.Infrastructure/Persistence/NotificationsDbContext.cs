using Microsoft.EntityFrameworkCore;
using Notifications.Domain.Entities;

namespace Notifications.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext scoped to the Notifications microservice, managing only notification-related entities.
/// </summary>
public sealed class NotificationsDbContext : DbContext
{
    public DbSet<FailedEmail> FailedEmails => Set<FailedEmail>();

    public NotificationsDbContext(DbContextOptions<NotificationsDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationsDbContext).Assembly);
    }
}
