using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedKernel.Infrastructure.Persistence.Configurations;
using Webhooks.Domain.Entities;

namespace Webhooks.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="WebhookSubscription"/> entity, applying standard
/// tenant-auditable conventions and relationship mappings.
/// </summary>
public sealed class WebhookSubscriptionConfiguration : IEntityTypeConfiguration<WebhookSubscription>
{
    public void Configure(EntityTypeBuilder<WebhookSubscription> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Url).IsRequired().HasMaxLength(WebhookSubscription.UrlMaxLength);

        builder
            .Property(e => e.Secret)
            .IsRequired()
            .HasMaxLength(WebhookSubscription.SecretMaxLength);

        builder.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);

        builder
            .HasMany(e => e.EventTypes)
            .WithOne()
            .HasForeignKey(et => et.WebhookSubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ConfigureTenantAuditable();
    }
}
