namespace Webhooks.Application.Common.DTOs;

/// <summary>
/// Represents an inbound webhook event that has been validated and enqueued for asynchronous processing.
/// </summary>
public sealed record InboundWebhookMessage(
    string EventType,
    string Payload,
    DateTimeOffset ReceivedAtUtc
);
