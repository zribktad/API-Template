namespace APITemplate.Application.Common.Contracts;

public interface IWebhookPayloadValidator
{
    bool IsValid(string payload, string signature, string timestamp);
}
