using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Webhooks.Domain.Entities;

namespace Webhooks.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="WebhookDeliveryLog"/> entity.
/// </summary>
public sealed class WebhookDeliveryLogConfiguration : IEntityTypeConfiguration<WebhookDeliveryLog>
{
    public void Configure(EntityTypeBuilder<WebhookDeliveryLog> builder)
    {
        builder.HasKey(e => e.Id);

        builder
            .Property(e => e.EventType)
            .IsRequired()
            .HasMaxLength(WebhookSubscription.EventTypeMaxLength);

        builder.Property(e => e.Payload).IsRequired();

        builder.Property(e => e.Error).HasMaxLength(WebhookDeliveryLog.ErrorMaxLength);

        builder
            .Property(e => e.AttemptedAtUtc)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(e => e.WebhookSubscriptionId);
        builder.HasIndex(e => e.AttemptedAtUtc);
    }
}
