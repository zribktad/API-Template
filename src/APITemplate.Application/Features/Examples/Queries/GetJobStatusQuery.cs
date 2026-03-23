using APITemplate.Application.Features.Examples.DTOs;
using APITemplate.Domain.Interfaces;

namespace APITemplate.Application.Features.Examples;

public sealed record GetJobStatusQuery(GetJobStatusRequest Request);

public sealed class GetJobStatusQueryHandler
{
    public static async Task<JobStatusResponse?> HandleAsync(
        GetJobStatusQuery query,
        IJobExecutionRepository repository,
        CancellationToken ct
    )
    {
        var entity = await repository.GetByIdAsync(query.Request.Id, ct);
        return entity is null ? null : JobResponseMapper.MapToResponse(entity);
    }
}
