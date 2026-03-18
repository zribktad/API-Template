using APITemplate.Domain.Enums;

namespace APITemplate.Application.Features.Examples.DTOs;

public sealed record JobStatusResponse(
    Guid Id,
    string JobType,
    JobStatus Status,
    int ProgressPercent,
    string? Parameters,
    string? ResultPayload,
    string? ErrorMessage,
    DateTime SubmittedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc
);
