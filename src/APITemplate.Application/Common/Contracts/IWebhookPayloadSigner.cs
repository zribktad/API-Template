namespace APITemplate.Application.Common.Contracts;

public interface IWebhookPayloadSigner
{
    WebhookSignatureResult Sign(string payload);
}

public sealed record WebhookSignatureResult(string Signature, string Timestamp);
