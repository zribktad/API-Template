using Microsoft.EntityFrameworkCore;
using Webhooks.Domain.Entities;

namespace Webhooks.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext scoped to the Webhooks microservice, managing webhook subscription
/// and delivery log entities.
/// </summary>
public sealed class WebhooksDbContext : DbContext
{
    public DbSet<WebhookSubscription> WebhookSubscriptions => Set<WebhookSubscription>();
    public DbSet<WebhookSubscriptionEventType> WebhookSubscriptionEventTypes =>
        Set<WebhookSubscriptionEventType>();
    public DbSet<WebhookDeliveryLog> WebhookDeliveryLogs => Set<WebhookDeliveryLog>();

    public WebhooksDbContext(DbContextOptions<WebhooksDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WebhooksDbContext).Assembly);
    }
}
