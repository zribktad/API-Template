using System.Security.Cryptography;
using System.Text;
using APITemplate.Application.Common.Contracts;
using APITemplate.Application.Common.Options;
using Microsoft.Extensions.Options;

namespace APITemplate.Infrastructure.Webhooks;

public sealed class HmacWebhookPayloadValidator : IWebhookPayloadValidator
{
    private readonly byte[] _keyBytes;
    private readonly int _toleranceSeconds;
    private readonly TimeProvider _timeProvider;

    public HmacWebhookPayloadValidator(IOptions<WebhookOptions> options, TimeProvider timeProvider)
    {
        _keyBytes = Encoding.UTF8.GetBytes(options.Value.Secret);
        _toleranceSeconds = options.Value.TimestampToleranceSeconds;
        _timeProvider = timeProvider;
    }

    public bool IsValid(string payload, string signature, string timestamp)
    {
        if (!long.TryParse(timestamp, out var unixSeconds))
            return false;

        var now = _timeProvider.GetUtcNow().ToUnixTimeSeconds();
        var delta = now > unixSeconds ? now - unixSeconds : unixSeconds - now;
        if (delta > _toleranceSeconds)
            return false;

        var signedContent = $"{timestamp}.{payload}";
        var contentBytes = Encoding.UTF8.GetBytes(signedContent);
        var hashBytes = HMACSHA256.HashData(_keyBytes, contentBytes);

        byte[] signatureBytes;
        try
        {
            signatureBytes = Convert.FromHexString(signature);
        }
        catch (FormatException)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(hashBytes, signatureBytes);
    }
}
