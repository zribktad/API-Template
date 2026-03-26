using SharedKernel.Domain.Entities;
using SharedKernel.Domain.Entities.Contracts;

namespace Webhooks.Domain.Entities;

/// <summary>
/// Represents a tenant-scoped webhook subscription that defines a callback URL,
/// a shared HMAC secret for signing deliveries, and the set of event types the subscriber is interested in.
/// </summary>
public sealed class WebhookSubscription : IAuditableTenantEntity, IHasId
{
    public const int UrlMaxLength = 2048;
    public const int SecretMinLength = 16;
    public const int SecretMaxLength = 256;
    public const int EventTypeMaxLength = 100;

    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    /// <summary>The HTTPS callback URL that webhook payloads are delivered to.</summary>
    public required string Url { get; set; }

    /// <summary>The shared HMAC-SHA256 secret used to sign outgoing deliveries to this subscriber.</summary>
    public required string Secret { get; set; }

    /// <summary>Whether this subscription is actively receiving deliveries.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Navigation property for the event types this subscription is interested in.</summary>
    public ICollection<WebhookSubscriptionEventType> EventTypes { get; set; } = [];

    // IAuditableEntity
    public AuditInfo Audit { get; set; } = new();

    // ISoftDeletable
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public Guid? DeletedBy { get; set; }
}
