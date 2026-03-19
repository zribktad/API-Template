using System.Text;
using APITemplate.Application.Common.Contracts;
using APITemplate.Application.Common.Options;
using Microsoft.Extensions.Options;

namespace APITemplate.Infrastructure.Webhooks;

public sealed class HmacWebhookPayloadSigner : IWebhookPayloadSigner
{
    private readonly byte[] _keyBytes;
    private readonly TimeProvider _timeProvider;

    public HmacWebhookPayloadSigner(IOptions<WebhookOptions> options, TimeProvider timeProvider)
    {
        _keyBytes = Encoding.UTF8.GetBytes(options.Value.Secret);
        _timeProvider = timeProvider;
    }

    public WebhookSignatureResult Sign(string payload)
    {
        var timestamp = _timeProvider.GetUtcNow().ToUnixTimeSeconds().ToString();
        var hashBytes = HmacHelper.ComputeHash(_keyBytes, timestamp, payload);
        var signature = Convert.ToHexStringLower(hashBytes);

        return new WebhookSignatureResult(signature, timestamp);
    }
}
