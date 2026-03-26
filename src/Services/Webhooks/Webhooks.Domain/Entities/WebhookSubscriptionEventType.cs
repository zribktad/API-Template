namespace Webhooks.Domain.Entities;

/// <summary>
/// Value-type child record representing a single event type that a <see cref="WebhookSubscription"/> listens to.
/// Stored as a separate table to support efficient filtering by event type.
/// </summary>
public sealed class WebhookSubscriptionEventType
{
    public Guid Id { get; set; }
    public Guid WebhookSubscriptionId { get; set; }

    /// <summary>The event type string (e.g. "product.created", "review.created").</summary>
    public required string EventType { get; set; }
}
