namespace BackgroundJobs.Infrastructure.StoredProcedures;

/// <summary>
/// Claims failed emails whose lease has expired (e.g. the processing node crashed),
/// re-assigning them to the current claim owner for another retry attempt.
///
/// Result: rows with columns matching the <c>FailedEmails</c> table schema.
/// </summary>
public sealed record ClaimExpiredFailedEmailsProcedure(
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
                  AND "ClaimedUntilUtc" IS NOT NULL
                  AND "ClaimedUntilUtc" < now()
                ORDER BY "ClaimedUntilUtc"
                FOR UPDATE SKIP LOCKED
                LIMIT @batchSize
            )
            RETURNING "Id", "To", "Subject", "HtmlBody", "RetryCount", "CreatedAtUtc",
                      "LastAttemptAtUtc", "LastError", "TemplateName", "IsDeadLettered",
                      "ClaimedBy", "ClaimedAtUtc", "ClaimedUntilUtc";
            """;
}
