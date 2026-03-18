using System.Text.Json;
using APITemplate.Application.Common.BackgroundJobs;
using APITemplate.Application.Features.Examples.DTOs;
using APITemplate.Domain.Entities;
using APITemplate.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace APITemplate.Infrastructure.BackgroundJobs.Services;

public sealed class JobProcessingBackgroundService : BackgroundService
{
    private const int SimulatedStepCount = 5;
    private const int SimulatedStepDelayMs = 200;
    private const int ProgressPerStep = 20;
    private const string CompletedResultSummary = "Job completed successfully";

    private readonly IJobQueueReader _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOutgoingWebhookQueue _outgoingWebhookQueue;
    private readonly ILogger<JobProcessingBackgroundService> _logger;
    private readonly TimeProvider _timeProvider;

    public JobProcessingBackgroundService(
        IJobQueueReader queue,
        IServiceScopeFactory scopeFactory,
        IOutgoingWebhookQueue outgoingWebhookQueue,
        ILogger<JobProcessingBackgroundService> logger,
        TimeProvider timeProvider
    )
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _outgoingWebhookQueue = outgoingWebhookQueue;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var jobId in _queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessJobAsync(jobId, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Job {JobId} failed", jobId);
                await TryMarkFailedAsync(jobId, ex.Message);
            }
        }
    }

    private async Task ProcessJobAsync(Guid jobId, CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IJobExecutionRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var job = await repo.GetByIdAsync(jobId, ct);
        if (job is null)
            return;

        job.MarkProcessing(_timeProvider);
        await uow.CommitAsync(ct);

        // Example: simulated multi-step job processing
        for (var step = 1; step <= SimulatedStepCount; step++)
        {
            await Task.Delay(SimulatedStepDelayMs, ct);
            job.UpdateProgress(step * ProgressPerStep);
            await uow.CommitAsync(ct);
        }

        job.MarkCompleted(
            JsonSerializer.Serialize(new { summary = CompletedResultSummary }),
            _timeProvider
        );
        await uow.CommitAsync(ct);

        await EnqueueCallbackAsync(job, ct);
    }

    private async Task TryMarkFailedAsync(Guid jobId, string errorMessage)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var repo = scope.ServiceProvider.GetRequiredService<IJobExecutionRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var job = await repo.GetByIdAsync(jobId, CancellationToken.None);
            if (job is not null)
            {
                job.MarkFailed(errorMessage, _timeProvider);
                await uow.CommitAsync(CancellationToken.None);

                await EnqueueCallbackAsync(job, CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark job {JobId} as failed", jobId);
        }
    }

    private async Task EnqueueCallbackAsync(JobExecution job, CancellationToken ct)
    {
        if (job.CallbackUrl is null)
            return;

        var payload = new OutgoingJobWebhookPayload(
            job.Id,
            job.JobType,
            job.Status.ToString(),
            job.ResultPayload,
            job.ErrorMessage,
            job.CompletedAtUtc ?? _timeProvider.GetUtcNow().UtcDateTime
        );

        var serialized = JsonSerializer.Serialize(payload, JsonSerializerOptions.Web);
        var item = new OutgoingWebhookItem(job.CallbackUrl, serialized);

        await _outgoingWebhookQueue.EnqueueAsync(item, ct);
    }
}
