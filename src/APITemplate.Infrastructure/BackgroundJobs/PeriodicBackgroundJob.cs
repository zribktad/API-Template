using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace APITemplate.Infrastructure.BackgroundJobs;

public abstract class PeriodicBackgroundJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger _logger;
    private readonly TimeSpan _interval;
    private readonly string _jobName;

    protected PeriodicBackgroundJob(
        IServiceScopeFactory scopeFactory,
        ILogger logger,
        TimeSpan interval,
        string jobName
    )
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _interval = interval;
        _jobName = jobName;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("{JobName} started with interval {Interval}.", _jobName, _interval);

        using var timer = new PeriodicTimer(_interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            _logger.LogInformation("{JobName} executing...", _jobName);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                using var scope = _scopeFactory.CreateScope();
                await ExecuteJobAsync(scope.ServiceProvider, stoppingToken);

                stopwatch.Stop();
                _logger.LogInformation(
                    "{JobName} completed in {ElapsedMs}ms.",
                    _jobName,
                    stopwatch.ElapsedMilliseconds
                );
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                stopwatch.Stop();
                _logger.LogError(
                    ex,
                    "{JobName} failed after {ElapsedMs}ms.",
                    _jobName,
                    stopwatch.ElapsedMilliseconds
                );
            }
        }
    }

    protected abstract Task ExecuteJobAsync(IServiceProvider serviceProvider, CancellationToken ct);
}
