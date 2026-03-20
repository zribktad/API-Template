using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Features.Examples.DTOs;
using APITemplate.Domain.Interfaces;

namespace APITemplate.Application.Features.Examples;

public sealed record GetJobStatusQuery(GetJobStatusRequest Request) : IQuery<JobStatusResponse?>;

public sealed class GetJobStatusQueryHandler : IQueryHandler<GetJobStatusQuery, JobStatusResponse?>
{
    private readonly IJobExecutionRepository _repository;

    public GetJobStatusQueryHandler(IJobExecutionRepository repository) => _repository = repository;

    public async Task<JobStatusResponse?> HandleAsync(GetJobStatusQuery query, CancellationToken ct)
    {
        var entity = await _repository.GetByIdAsync(query.Request.Id, ct);
        return entity is null ? null : JobResponseMapper.MapToResponse(entity);
    }
}
