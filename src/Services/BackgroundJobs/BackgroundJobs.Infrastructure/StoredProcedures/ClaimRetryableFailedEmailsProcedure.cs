namespace BackgroundJobs.Infrastructure.StoredProcedures;

/// <summary>
/// Claims a batch of failed emails eligible for retry by atomically setting <c>ClaimedBy</c>,
/// <c>ClaimedAtUtc</c>, and <c>ClaimedUntilUtc</c> on unclaimed rows that have not exceeded
/// the maximum retry count or been dead-lettered.
///
/// Result: rows with columns matching the <c>FailedEmails</c> table schema.
/// </summary>
public sealed record ClaimRetryableFailedEmailsProcedure(
    int MaxRetryAttempts,
    int BatchSize,
    string ClaimOwner,
    DateTime ClaimUntilUtc
)
{
    public string ToSql() =>
        """
            UPDATE "FailedEmails"
            SET "ClaimedBy" = @claimOwner,
                "ClaimedAtUtc" = now(),
                "ClaimedUntilUtc" = @claimUntilUtc
            WHERE "Id" IN (
                SELECT "Id"
                FROM "FailedEmails"
                WHERE "IsDeadLettered" = false
                  AND "RetryCount" < @maxRetryAttempts
                  AND ("ClaimedUntilUtc" IS NULL OR "ClaimedUntilUtc" < now())
                ORDER BY "LastAttemptAtUtc" NULLS FIRST
                FOR UPDATE SKIP LOCKED
                LIMIT @batchSize
            )
            RETURNING "Id", "To", "Subject", "HtmlBody", "RetryCount", "CreatedAtUtc",
                      "LastAttemptAtUtc", "LastError", "TemplateName", "IsDeadLettered",
                      "ClaimedBy", "ClaimedAtUtc", "ClaimedUntilUtc";
            """;
}
