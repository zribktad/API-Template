using System.Security.Cryptography;
using System.Text;
using Webhooks.Application.Common.Contracts;

namespace Webhooks.Infrastructure.Hmac;

/// <summary>
/// Validates inbound webhook payload signatures using HMAC-SHA256, ensuring both the signature
/// matches and the timestamp is within the allowed tolerance window to prevent replay attacks.
/// </summary>
public sealed class HmacWebhookPayloadValidator : IWebhookPayloadValidator
{
    private const int MaxTimestampDriftSeconds = 300;

    private readonly TimeProvider _timeProvider;

    public HmacWebhookPayloadValidator(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public bool Validate(string payload, string signature, string timestamp, string secret)
    {
        if (!long.TryParse(timestamp, out long unixSeconds))
        {
            return false;
        }

        long nowUnix = _timeProvider.GetUtcNow().ToUnixTimeSeconds();
        if (Math.Abs(nowUnix - unixSeconds) > MaxTimestampDriftSeconds)
        {
            return false;
        }

        byte[] keyBytes = Encoding.UTF8.GetBytes(secret);
        byte[] expectedHash = HmacHelper.ComputeHash(keyBytes, timestamp, payload);
        byte[] actualHash = Convert.FromHexString(signature);

        return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
    }
}
