using BackgroundJobs.Application.Features.Jobs.DTOs;
using BackgroundJobs.Application.Features.Jobs.Mappings;
using BackgroundJobs.Domain.Entities;
using BackgroundJobs.Domain.Interfaces;
using ErrorOr;
using SharedKernel.Application.Errors;

namespace BackgroundJobs.Application.Features.Jobs.Queries;

public sealed record GetJobStatusQuery(GetJobStatusRequest Request);

public sealed class GetJobStatusQueryHandler
{
    public static async Task<ErrorOr<JobStatusResponse>> HandleAsync(
        GetJobStatusQuery query,
        IJobExecutionRepository repository,
        CancellationToken ct
    )
    {
        JobExecution? entity = await repository.GetByIdAsync(query.Request.Id, ct);
        return entity is null
            ? DomainErrors.General.NotFound("JobExecution", query.Request.Id)
            : JobResponseMapper.MapToResponse(entity);
    }
}
