namespace Webhooks.Application.Common.Constants;

/// <summary>
/// Named constants for the webhook event types that this service can deliver.
/// </summary>
public static class WebhookEventTypes
{
    public const string ProductCreated = "product.created";
    public const string ProductDeleted = "product.deleted";
    public const string ReviewCreated = "review.created";
}
