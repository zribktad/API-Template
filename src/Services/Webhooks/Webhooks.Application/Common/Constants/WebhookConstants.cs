namespace Webhooks.Application.Common.Constants;

/// <summary>
/// Centralizes header names and HTTP client identifiers used by the webhook delivery infrastructure.
/// </summary>
public static class WebhookConstants
{
    public const string SignatureHeader = "X-Webhook-Signature";
    public const string TimestampHeader = "X-Webhook-Timestamp";
    public const string OutgoingHttpClientName = "OutgoingWebhook";
}
