namespace APITemplate.Application.Features.Examples.DTOs;

public sealed record ReceiveWebhookRequest(string Body, string Signature, string Timestamp);
