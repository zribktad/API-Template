namespace Webhooks.Application.Common.Contracts;

/// <summary>
/// Application-layer abstraction for delivering webhook payloads to all active subscribers
/// that are registered for a given event type.
/// </summary>
public interface IWebhookDeliveryService
{
    /// <summary>
    /// Signs and delivers the <paramref name="serializedPayload"/> to all active subscribers
    /// that are registered for the specified <paramref name="eventType"/>.
    /// </summary>
    Task DeliverAsync(
        string eventType,
        string serializedPayload,
        Guid tenantId,
        CancellationToken ct = default
    );
}
