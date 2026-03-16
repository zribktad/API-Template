using APITemplate.Application.Common.BackgroundJobs;
using APITemplate.Application.Common.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace APITemplate.Infrastructure.BackgroundJobs;

public sealed class EmailRetryBackgroundJob : PeriodicBackgroundJob
{
    private readonly EmailRetryJobOptions _options;

    public EmailRetryBackgroundJob(
        IServiceScopeFactory scopeFactory,
        ILogger<EmailRetryBackgroundJob> logger,
        IOptions<BackgroundJobsOptions> options
    )
        : base(
            scopeFactory,
            logger,
            TimeSpan.FromMinutes(options.Value.EmailRetry.IntervalMinutes),
            nameof(EmailRetryBackgroundJob)
        )
    {
        _options = options.Value.EmailRetry;
    }

    protected override async Task ExecuteJobAsync(
        IServiceProvider serviceProvider,
        CancellationToken ct
    )
    {
        var service = serviceProvider.GetRequiredService<IEmailRetryService>();

        await service.RetryFailedEmailsAsync(_options.MaxRetryAttempts, _options.BatchSize, ct);
        await service.DeadLetterExpiredAsync(_options.DeadLetterAfterHours, _options.BatchSize, ct);
    }
}
