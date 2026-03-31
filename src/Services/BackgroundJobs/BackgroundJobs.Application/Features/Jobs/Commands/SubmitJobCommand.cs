using BackgroundJobs.Application.Common;
using BackgroundJobs.Application.Features.Jobs.DTOs;
using BackgroundJobs.Application.Features.Jobs.Mappings;
using BackgroundJobs.Domain.Entities;
using BackgroundJobs.Domain.Interfaces;
using ErrorOr;
using SharedKernel.Application.Context;
using SharedKernel.Domain.Interfaces;

namespace BackgroundJobs.Application.Features.Jobs.Commands;

public sealed record SubmitJobCommand(SubmitJobRequest Request);

public sealed class SubmitJobCommandHandler
{
    public static async Task<ErrorOr<JobStatusResponse>> HandleAsync(
        SubmitJobCommand command,
        IJobExecutionRepository repository,
        IJobQueue jobQueue,
        IUnitOfWork unitOfWork,
        ITenantProvider tenantProvider,
        TimeProvider timeProvider,
        CancellationToken ct
    )
    {
        JobExecution entity = new()
        {
            Id = Guid.NewGuid(),
            JobType = command.Request.JobType,
            Parameters = command.Request.Parameters,
            CallbackUrl = command.Request.CallbackUrl,
            SubmittedAtUtc = timeProvider.GetUtcNow().UtcDateTime,
            TenantId = tenantProvider.TenantId,
        };

        await unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                await repository.AddAsync(entity, ct);
            },
            ct
        );

        await jobQueue.EnqueueAsync(entity.Id, ct);

        return JobResponseMapper.MapToResponse(entity);
    }
}
