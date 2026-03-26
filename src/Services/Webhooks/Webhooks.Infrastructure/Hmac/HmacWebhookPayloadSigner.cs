using System.Text;
using Webhooks.Application.Common.Contracts;

namespace Webhooks.Infrastructure.Hmac;

/// <summary>
/// Signs outgoing webhook payloads using HMAC-SHA256 with a per-subscription secret, producing
/// a signature and UTC Unix timestamp for inclusion in request headers.
/// </summary>
public sealed class HmacWebhookPayloadSigner : IWebhookPayloadSigner
{
    private readonly TimeProvider _timeProvider;

    public HmacWebhookPayloadSigner(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    /// <summary>Computes the HMAC-SHA256 signature over the current timestamp and payload, returning both values as a <see cref="WebhookSignatureResult"/>.</summary>
    public WebhookSignatureResult Sign(string payload, string secret)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(secret);
        string timestamp = _timeProvider.GetUtcNow().ToUnixTimeSeconds().ToString();
        byte[] hashBytes = HmacHelper.ComputeHash(keyBytes, timestamp, payload);
        string signature = Convert.ToHexStringLower(hashBytes);

        return new WebhookSignatureResult(signature, timestamp);
    }
}
