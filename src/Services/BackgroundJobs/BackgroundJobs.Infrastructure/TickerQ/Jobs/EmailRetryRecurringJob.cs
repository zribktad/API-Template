using BackgroundJobs.Application.Common;
using BackgroundJobs.Infrastructure.TickerQ.Coordination;
using Microsoft.Extensions.Logging;
using TickerQ.Utilities.Base;

namespace BackgroundJobs.Infrastructure.TickerQ.Jobs;

/// <summary>
/// TickerQ recurring job that retries failed emails from the Notifications database
/// through <see cref="IEmailRetryService"/>.
/// Execution is gated by <see cref="IDistributedJobCoordinator"/> to prevent multi-node duplication.
/// </summary>
public sealed class EmailRetryRecurringJob
{
    private readonly IEmailRetryService _emailRetryService;
    private readonly IDistributedJobCoordinator _coordinator;
    private readonly ILogger<EmailRetryRecurringJob> _logger;

    public EmailRetryRecurringJob(
        IEmailRetryService emailRetryService,
        IDistributedJobCoordinator coordinator,
        ILogger<EmailRetryRecurringJob> logger
    )
    {
        _emailRetryService = emailRetryService;
        _coordinator = coordinator;
        _logger = logger;
    }

    /// <summary>TickerQ entry-point that acquires the distributed leader lease and invokes the email retry service.</summary>
    [TickerFunction(TickerQFunctionNames.EmailRetry)]
    public Task ExecuteAsync(TickerFunctionContext context, CancellationToken ct) =>
        _coordinator.ExecuteIfLeaderAsync(
            TickerQFunctionNames.EmailRetry,
            async token =>
            {
                _logger.LogInformation(
                    "Executing email retry recurring job for ticker {TickerId}.",
                    context.Id
                );
                await _emailRetryService.RetryFailedEmailsAsync(token);
            },
            ct
        );
}
