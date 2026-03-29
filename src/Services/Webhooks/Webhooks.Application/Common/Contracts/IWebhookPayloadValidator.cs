namespace Webhooks.Application.Common.Contracts;

/// <summary>
/// Application-layer abstraction for validating inbound webhook payload signatures,
/// ensuring authenticity and protection against replay attacks.
/// </summary>
public interface IWebhookPayloadValidator
{
    /// <summary>
    /// Validates the HMAC signature of an inbound webhook payload using the provided
    /// <paramref name="secret"/>, returning <c>true</c> when the signature and timestamp are valid.
    /// </summary>
    bool Validate(string payload, string signature, string timestamp, string secret);
}
