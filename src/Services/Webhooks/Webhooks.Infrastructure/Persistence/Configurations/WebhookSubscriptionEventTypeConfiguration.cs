using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Webhooks.Domain.Entities;

namespace Webhooks.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="WebhookSubscriptionEventType"/> entity.
/// </summary>
public sealed class WebhookSubscriptionEventTypeConfiguration
    : IEntityTypeConfiguration<WebhookSubscriptionEventType>
{
    public void Configure(EntityTypeBuilder<WebhookSubscriptionEventType> builder)
    {
        builder.HasKey(e => e.Id);

        builder
            .Property(e => e.EventType)
            .IsRequired()
            .HasMaxLength(WebhookSubscription.EventTypeMaxLength);

        builder.HasIndex(e => e.EventType);
        builder.HasIndex(e => new { e.WebhookSubscriptionId, e.EventType }).IsUnique();
    }
}
