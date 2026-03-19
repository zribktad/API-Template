using APITemplate.Application.Common.BackgroundJobs;
using APITemplate.Application.Common.Errors;
using APITemplate.Application.Features.Examples.DTOs;
using APITemplate.Domain.Entities;
using APITemplate.Domain.Exceptions;
using APITemplate.Domain.Interfaces;
using MediatR;

namespace APITemplate.Application.Features.Examples.Handlers;

/// <summary>Enqueues a new background job described by the inner <see cref="SubmitJobRequest"/>.</summary>
public sealed record SubmitJobCommand(SubmitJobRequest Request) : IRequest<JobStatusResponse>;

/// <summary>Retrieves the current execution status for the job identified inside <see cref="GetJobStatusRequest"/>.</summary>
public sealed record GetJobStatusQuery(GetJobStatusRequest Request) : IRequest<JobStatusResponse?>;

/// <summary>
/// Application-layer handler that creates and persists a <c>JobExecution</c> entity, enqueues it for background processing after the transaction commits, and serves status-poll queries.
/// </summary>
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

    /// <summary>Persists the job entity in a transaction and enqueues it after commit so the processor can always find the record.</summary>
    public async Task<JobStatusResponse> Handle(SubmitJobCommand command, CancellationToken ct)
    {
        var entity = new JobExecution
        {
            Id = Guid.NewGuid(),
            JobType = command.Request.JobType,
            Parameters = command.Request.Parameters,
            CallbackUrl = command.Request.CallbackUrl,
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

    /// <summary>Returns the current job status, or <see langword="null"/> if no job with the given ID exists.</summary>
    public async Task<JobStatusResponse?> Handle(GetJobStatusQuery query, CancellationToken ct)
    {
        var entity = await _repository.GetByIdAsync(query.Request.Id, ct);
        return entity is null ? null : MapToResponse(entity);
    }

    /// <summary>Projects a <c>JobExecution</c> entity to its response DTO.</summary>
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
            entity.CompletedAtUtc,
            entity.CallbackUrl
        );
}
