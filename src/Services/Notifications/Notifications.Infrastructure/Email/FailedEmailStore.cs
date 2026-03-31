using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Notifications.Domain.Entities;
using Notifications.Domain.Interfaces;
using Notifications.Domain.ValueObjects;

namespace Notifications.Infrastructure.Email;

/// <summary>
/// Infrastructure implementation of <see cref="IFailedEmailStore"/> that persists a <see cref="FailedEmail"/>
/// record when delivery fails, provided the email is marked retryable.
/// Uses a new DI scope per call to avoid captive-dependency issues with scoped services.
/// </summary>
public sealed class FailedEmailStore : IFailedEmailStore
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FailedEmailStore> _logger;

    public FailedEmailStore(IServiceScopeFactory scopeFactory, ILogger<FailedEmailStore> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Persists a new <see cref="FailedEmail"/> for <paramref name="message"/> if the message is
    /// retryable; silently swallows storage errors to avoid masking the original send failure.
    /// </summary>
    public async Task StoreFailedAsync(
        EmailMessage message,
        string error,
        CancellationToken ct = default
    )
    {
        if (!message.Retryable)
        {
            return;
        }

        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            IFailedEmailRepository repository =
                scope.ServiceProvider.GetRequiredService<IFailedEmailRepository>();
            TimeProvider timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();

            FailedEmail failedEmail = new()
            {
                Id = Guid.NewGuid(),
                To = message.To,
                Subject = message.Subject,
                HtmlBody = message.HtmlBody,
                RetryCount = 0,
                CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime,
                LastError = FailedEmailErrorNormalizer.Normalize(error),
                TemplateName = message.TemplateName,
                ClaimedBy = null,
                ClaimedAtUtc = null,
                ClaimedUntilUtc = null,
            };

            await repository.AddAsync(failedEmail, ct);
            await repository.SaveChangesAsync(ct);

            _logger.LogWarning(
                "Stored failed email to {Recipient} with subject '{Subject}' for retry.",
                message.To,
                message.Subject
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to store failed email to {Recipient} for retry.",
                message.To
            );
        }
    }
}
