using APITemplate.Application.Common.BackgroundJobs;
using Microsoft.Extensions.Hosting;

namespace APITemplate.Infrastructure.BackgroundJobs.Services;

public abstract class QueueConsumerBackgroundService<T> : BackgroundService
{
    private readonly IQueueReader<T> _queue;

    protected QueueConsumerBackgroundService(IQueueReader<T> queue) => _queue = queue;

    protected sealed override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var item in _queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessItemAsync(item, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await HandleErrorAsync(item, ex, stoppingToken);
            }
        }
    }

    protected abstract Task ProcessItemAsync(T item, CancellationToken ct);

    protected virtual Task HandleErrorAsync(T item, Exception ex, CancellationToken ct) =>
        Task.CompletedTask;
}
