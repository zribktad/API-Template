using APITemplate.Application.Common.BackgroundJobs;
using APITemplate.Application.Common.Errors;
using APITemplate.Application.Features.Examples.DTOs;
using APITemplate.Domain.Entities;
using APITemplate.Domain.Exceptions;
using APITemplate.Domain.Interfaces;
using MediatR;

namespace APITemplate.Application.Features.Examples.Handlers;

public sealed record SubmitJobCommand(SubmitJobRequest Request) : IRequest<JobStatusResponse>;

public sealed record GetJobStatusQuery(Guid Id) : IRequest<JobStatusResponse?>;

public sealed class JobRequestHandlers
    : IRequestHandler<SubmitJobCommand, JobStatusResponse>,
        IRequestHandler<GetJobStatusQuery, JobStatusResponse?>
{
    private readonly IJobExecutionRepository _repository;
    private readonly IJobQueue _jobQueue;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public JobRequestHandlers(
        IJobExecutionRepository repository,
        IJobQueue jobQueue,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider
    )
    {
        _repository = repository;
        _jobQueue = jobQueue;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<JobStatusResponse> Handle(SubmitJobCommand command, CancellationToken ct)
    {
        var entity = new JobExecution
        {
            Id = Guid.NewGuid(),
            JobType = command.Request.JobType,
            Parameters = command.Request.Parameters,
            SubmittedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
        };

        await _unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                await _repository.AddAsync(entity, ct);
            },
            ct
        );

        // Enqueue after commit so the background processor can always find the persisted entity.
        await _jobQueue.EnqueueAsync(entity.Id, ct);

        return MapToResponse(entity);
    }

    public async Task<JobStatusResponse?> Handle(GetJobStatusQuery query, CancellationToken ct)
    {
        var entity = await _repository.GetByIdAsync(query.Id, ct);
        return entity is null ? null : MapToResponse(entity);
    }

    private static JobStatusResponse MapToResponse(JobExecution entity) =>
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
            entity.CompletedAtUtc
        );
}
