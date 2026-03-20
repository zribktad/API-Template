using APITemplate.Application.Common.BackgroundJobs;
using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Features.Examples.DTOs;
using APITemplate.Domain.Entities;
using APITemplate.Domain.Interfaces;

namespace APITemplate.Application.Features.Examples;

public sealed record SubmitJobCommand(SubmitJobRequest Request) : ICommand<JobStatusResponse>;

public sealed class SubmitJobCommandHandler : ICommandHandler<SubmitJobCommand, JobStatusResponse>
{
    private readonly IJobExecutionRepository _repository;
    private readonly IJobQueue _jobQueue;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public SubmitJobCommandHandler(
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

    public async Task<JobStatusResponse> HandleAsync(SubmitJobCommand command, CancellationToken ct)
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

        await _jobQueue.EnqueueAsync(entity.Id, ct);

        return JobResponseMapper.MapToResponse(entity);
    }
}

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
