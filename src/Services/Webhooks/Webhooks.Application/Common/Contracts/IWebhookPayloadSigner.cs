namespace Webhooks.Application.Common.Contracts;

/// <summary>
/// Application-layer abstraction for signing outgoing webhook payloads so that receivers can
/// verify authenticity. Implementations provide the HMAC or similar signing algorithm.
/// </summary>
public interface IWebhookPayloadSigner
{
    /// <summary>
    /// Computes a signature and timestamp for the given <paramref name="payload"/> string
    /// using the specified <paramref name="secret"/>.
    /// </summary>
    WebhookSignatureResult Sign(string payload, string secret);
}

/// <summary>
/// Value object containing the computed HMAC signature and the timestamp used as the signing input,
/// both of which are included as HTTP headers on outgoing webhook deliveries.
/// </summary>
public sealed record WebhookSignatureResult(string Signature, string Timestamp);
