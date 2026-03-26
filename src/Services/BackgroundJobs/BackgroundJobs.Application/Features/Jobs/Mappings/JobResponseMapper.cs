using BackgroundJobs.Application.Features.Jobs.DTOs;
using BackgroundJobs.Domain.Entities;

namespace BackgroundJobs.Application.Features.Jobs.Mappings;

internal static class JobResponseMapper
{
    internal static JobStatusResponse MapToResponse(JobExecution entity) =>
        new(
            entity.Id,
            entity.JobType,
            entity.Status,
            entity.ProgressPercent,
            entity.Parameters,
            entity.ResultPayload,
            entity.ErrorMessage,
            entity.SubmittedAtUtc,
            entity.StartedAtUtc,
            entity.CompletedAtUtc,
            entity.CallbackUrl
        );
}
