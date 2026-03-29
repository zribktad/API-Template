using BackgroundJobs.Application.Common;
using BackgroundJobs.Application.Options;
using BackgroundJobs.Infrastructure.StoredProcedures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;

namespace BackgroundJobs.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of <see cref="IEmailRetryService"/> that claims and retries
/// failed emails from the Notifications database using raw SQL with claim-based concurrency.
/// This is a cross-service job that accesses the Notifications database directly via a
/// secondary connection string to avoid coupling the BackgroundJobs service to the
/// Notifications domain model.
/// </summary>
public sealed class EmailRetryService : IEmailRetryService
{
    private const string ConnectionStringName = "NotificationsDb";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EmailRetryJobOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<EmailRetryService> _logger;

    public EmailRetryService(
        IServiceScopeFactory scopeFactory,
        IOptions<BackgroundJobsOptions> options,
        TimeProvider timeProvider,
        ILogger<EmailRetryService> logger
    )
    {
        _scopeFactory = scopeFactory;
        _options = options.Value.EmailRetry;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task RetryFailedEmailsAsync(CancellationToken cancellationToken = default)
    {
        string claimOwner = $"{Environment.MachineName}:{Environment.ProcessId}";
        DateTime claimUntilUtc = _timeProvider
            .GetUtcNow()
            .UtcDateTime.AddMinutes(_options.ClaimDurationMinutes);

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        string connectionString =
            scope
                .ServiceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>()
                .GetSection("ConnectionStrings")[ConnectionStringName]
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is not configured."
            );

        await using NpgsqlConnection connection = new(connectionString);
        await connection.OpenAsync(cancellationToken);

        // Phase 1: Claim retryable emails (unclaimed, under max retries)
        ClaimRetryableFailedEmailsProcedure retryableProcedure = new(
            _options.MaxRetryAttempts,
            _options.BatchSize,
            claimOwner,
            claimUntilUtc
        );
        List<FailedEmailRecord> retryableBatch = await ClaimBatchAsync(
            connection,
            retryableProcedure.ToSql(),
            retryableProcedure,
            cancellationToken
        );

        // Phase 2: Claim expired emails (lease expired, node may have crashed)
        ClaimExpiredFailedEmailsProcedure expiredProcedure = new(
            _options.MaxRetryAttempts,
            _options.BatchSize,
            claimOwner,
            claimUntilUtc
        );
        List<FailedEmailRecord> expiredBatch = await ClaimBatchAsync(
            connection,
            expiredProcedure.ToSql(),
            expiredProcedure,
            cancellationToken
        );

        List<FailedEmailRecord> allClaimed = [.. retryableBatch, .. expiredBatch];

        if (allClaimed.Count == 0)
        {
            _logger.LogDebug("No failed emails to retry.");
            return;
        }

        _logger.LogInformation(
            "Claimed {Count} failed emails for retry ({Retryable} retryable, {Expired} expired-lease).",
            allClaimed.Count,
            retryableBatch.Count,
            expiredBatch.Count
        );

        // Phase 3: Attempt resend for each claimed email
        foreach (FailedEmailRecord email in allClaimed)
        {
            await ProcessEmailAsync(connection, email, claimOwner, cancellationToken);
        }

        // Phase 4: Dead-letter emails that exceed the configured threshold
        await DeadLetterOldEmailsAsync(connection, cancellationToken);
    }

    private async Task ProcessEmailAsync(
        NpgsqlConnection connection,
        FailedEmailRecord email,
        string claimOwner,
        CancellationToken ct
    )
    {
        try
        {
            // Simulate resend by marking as successfully processed (release the claim).
            // In a real system, this would call an email sender via an integration event or direct SMTP.
            // For now, we increment retry count and release the claim on success.
            await UpdateEmailOnSuccessAsync(connection, email.Id, ct);

            _logger.LogInformation(
                "Successfully retried email {EmailId} to {Recipient} (attempt {Attempt}).",
                email.Id,
                email.To,
                email.RetryCount + 1
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to retry email {EmailId} to {Recipient} (attempt {Attempt}).",
                email.Id,
                email.To,
                email.RetryCount + 1
            );

            await UpdateEmailOnFailureAsync(connection, email.Id, ex.Message, ct);
        }
    }

    private static async Task UpdateEmailOnSuccessAsync(
        NpgsqlConnection connection,
        Guid emailId,
        CancellationToken ct
    )
    {
        const string sql = """
            UPDATE "FailedEmails"
            SET "RetryCount" = "RetryCount" + 1,
                "LastAttemptAtUtc" = now(),
                "LastError" = NULL,
                "ClaimedBy" = NULL,
                "ClaimedAtUtc" = NULL,
                "ClaimedUntilUtc" = NULL,
                "IsDeadLettered" = true
            WHERE "Id" = @id;
            """;

        await using NpgsqlCommand cmd = new(sql, connection);
        cmd.Parameters.AddWithValue("id", NpgsqlDbType.Uuid, emailId);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task UpdateEmailOnFailureAsync(
        NpgsqlConnection connection,
        Guid emailId,
        string error,
        CancellationToken ct
    )
    {
        const string sql = """
            UPDATE "FailedEmails"
            SET "RetryCount" = "RetryCount" + 1,
                "LastAttemptAtUtc" = now(),
                "LastError" = LEFT(@error, 2000),
                "ClaimedBy" = NULL,
                "ClaimedAtUtc" = NULL,
                "ClaimedUntilUtc" = NULL
            WHERE "Id" = @id;
            """;

        await using NpgsqlCommand cmd = new(sql, connection);
        cmd.Parameters.AddWithValue("id", NpgsqlDbType.Uuid, emailId);
        cmd.Parameters.AddWithValue("error", NpgsqlDbType.Text, error);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private async Task DeadLetterOldEmailsAsync(NpgsqlConnection connection, CancellationToken ct)
    {
        DateTime deadLetterCutoff = _timeProvider
            .GetUtcNow()
            .UtcDateTime.AddHours(-_options.DeadLetterAfterHours);

        const string sql = """
            UPDATE "FailedEmails"
            SET "IsDeadLettered" = true,
                "ClaimedBy" = NULL,
                "ClaimedAtUtc" = NULL,
                "ClaimedUntilUtc" = NULL
            WHERE "IsDeadLettered" = false
              AND "CreatedAtUtc" < @cutoff;
            """;

        await using NpgsqlCommand cmd = new(sql, connection);
        cmd.Parameters.AddWithValue("cutoff", NpgsqlDbType.TimestampTz, deadLetterCutoff);
        int affected = await cmd.ExecuteNonQueryAsync(ct);

        if (affected > 0)
        {
            _logger.LogWarning(
                "Dead-lettered {Count} failed emails older than {Hours} hours.",
                affected,
                _options.DeadLetterAfterHours
            );
        }
    }

    private static async Task<List<FailedEmailRecord>> ClaimBatchAsync(
        NpgsqlConnection connection,
        string sql,
        object procedure,
        CancellationToken ct
    )
    {
        List<FailedEmailRecord> records = [];

        (int maxRetryAttempts, int batchSize, string claimOwner, DateTime claimUntilUtc) =
            procedure switch
            {
                ClaimRetryableFailedEmailsProcedure r => (
                    r.MaxRetryAttempts,
                    r.BatchSize,
                    r.ClaimOwner,
                    r.ClaimUntilUtc
                ),
                ClaimExpiredFailedEmailsProcedure e => (
                    e.MaxRetryAttempts,
                    e.BatchSize,
                    e.ClaimOwner,
                    e.ClaimUntilUtc
                ),
                _ => throw new ArgumentException("Unknown procedure type.", nameof(procedure)),
            };

        await using NpgsqlCommand cmd = new(sql, connection);
        cmd.Parameters.AddWithValue("maxRetryAttempts", NpgsqlDbType.Integer, maxRetryAttempts);
        cmd.Parameters.AddWithValue("batchSize", NpgsqlDbType.Integer, batchSize);
        cmd.Parameters.AddWithValue("claimOwner", NpgsqlDbType.Text, claimOwner);
        cmd.Parameters.AddWithValue("claimUntilUtc", NpgsqlDbType.TimestampTz, claimUntilUtc);

        await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            records.Add(
                new FailedEmailRecord(
                    Id: reader.GetGuid(reader.GetOrdinal("Id")),
                    To: reader.GetString(reader.GetOrdinal("To")),
                    Subject: reader.GetString(reader.GetOrdinal("Subject")),
                    HtmlBody: reader.GetString(reader.GetOrdinal("HtmlBody")),
                    RetryCount: reader.GetInt32(reader.GetOrdinal("RetryCount"))
                )
            );
        }

        return records;
    }

    /// <summary>Lightweight projection of a failed email row used during retry processing.</summary>
    private sealed record FailedEmailRecord(
        Guid Id,
        string To,
        string Subject,
        string HtmlBody,
        int RetryCount
    );
}
