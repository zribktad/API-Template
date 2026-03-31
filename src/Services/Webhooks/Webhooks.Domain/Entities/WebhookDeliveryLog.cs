using SharedKernel.Domain.Entities.Contracts;

namespace Webhooks.Domain.Entities;

/// <summary>
/// Immutable audit record of a single webhook delivery attempt, capturing the HTTP status code
/// and any error message for debugging and retry decisions.
/// </summary>
public sealed class WebhookDeliveryLog : IHasId
{
    public const int ErrorMaxLength = 2000;

    public Guid Id { get; set; }
    public Guid WebhookSubscriptionId { get; set; }

    /// <summary>The event type that triggered this delivery (e.g. "product.created").</summary>
    public required string EventType { get; set; }

    /// <summary>The serialized JSON payload that was delivered.</summary>
    public required string Payload { get; set; }

    /// <summary>HTTP status code returned by the subscriber's endpoint, or null if the request failed before a response.</summary>
    public int? HttpStatusCode { get; set; }

    /// <summary>Whether the delivery was successful (2xx status code).</summary>
    public bool Success { get; set; }

    /// <summary>Error message if the delivery failed.</summary>
    public string? Error { get; set; }

    public DateTime AttemptedAtUtc { get; set; }
}
