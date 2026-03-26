namespace Webhooks.Application.Features.Subscriptions.DTOs;

/// <summary>
/// Request payload for creating a new webhook subscription.
/// </summary>
public sealed record CreateWebhookSubscriptionRequest(
    string Url,
    string Secret,
    IReadOnlyList<string> EventTypes
);

/// <summary>
/// Response payload representing a webhook subscription.
/// </summary>
public sealed record WebhookSubscriptionResponse(
    Guid Id,
    string Url,
    bool IsActive,
    IReadOnlyList<string> EventTypes,
    DateTime CreatedAtUtc
);
