namespace APITemplate.Application.Features.Examples.DTOs;

public sealed record OutgoingWebhookItem(string CallbackUrl, string SerializedPayload);

public sealed record OutgoingJobWebhookPayload(
    Guid JobId,
    string JobType,
    string Status,
    string? ResultPayload,
    string? ErrorMessage,
    DateTime CompletedAtUtc
);
