using APITemplate.Domain.Entities;
using APITemplate.Domain.Interfaces;
using APITemplate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace APITemplate.Infrastructure.Repositories;

public sealed class FailedEmailRepository : IFailedEmailRepository
{
    private readonly AppDbContext _dbContext;

    public FailedEmailRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(FailedEmail failedEmail, CancellationToken ct = default)
    {
        _dbContext.FailedEmails.Add(failedEmail);
        return Task.CompletedTask;
    }

    public Task<List<FailedEmail>> ClaimRetryableBatchAsync(
        int maxRetryAttempts,
        int batchSize,
        string claimedBy,
        DateTime claimedAtUtc,
        DateTime claimedUntilUtc,
        CancellationToken ct = default
    ) =>
        ClaimBatchAsync(
            """
            WITH claimed AS (
                SELECT "Id"
                FROM "FailedEmails"
                WHERE NOT "IsDeadLettered"
                  AND "RetryCount" < @maxRetryAttempts
                  AND ("ClaimedUntilUtc" IS NULL OR "ClaimedUntilUtc" < @claimedAtUtc)
                ORDER BY COALESCE("LastAttemptAtUtc", "CreatedAtUtc")
                LIMIT @batchSize
                FOR UPDATE SKIP LOCKED
            )
            UPDATE "FailedEmails" AS failed
            SET "ClaimedBy" = @claimedBy,
                "ClaimedAtUtc" = @claimedAtUtc,
                "ClaimedUntilUtc" = @claimedUntilUtc
            FROM claimed
            WHERE failed."Id" = claimed."Id"
            RETURNING failed."Id", failed."To", failed."Subject", failed."HtmlBody",
                      failed."RetryCount", failed."CreatedAtUtc", failed."LastAttemptAtUtc",
                      failed."LastError", failed."TemplateName", failed."IsDeadLettered",
                      failed."ClaimedBy", failed."ClaimedAtUtc", failed."ClaimedUntilUtc";
            """,
            [
                new NpgsqlParameter("maxRetryAttempts", maxRetryAttempts),
                new NpgsqlParameter("batchSize", batchSize),
                new NpgsqlParameter("claimedBy", claimedBy),
                new NpgsqlParameter("claimedAtUtc", claimedAtUtc),
                new NpgsqlParameter("claimedUntilUtc", claimedUntilUtc),
            ],
            ct
        );

    public Task<List<FailedEmail>> ClaimExpiredBatchAsync(
        DateTime cutoff,
        int batchSize,
        string claimedBy,
        DateTime claimedAtUtc,
        DateTime claimedUntilUtc,
        CancellationToken ct = default
    ) =>
        ClaimBatchAsync(
            """
            WITH claimed AS (
                SELECT "Id"
                FROM "FailedEmails"
                WHERE NOT "IsDeadLettered"
                  AND "CreatedAtUtc" < @cutoff
                  AND ("ClaimedUntilUtc" IS NULL OR "ClaimedUntilUtc" < @claimedAtUtc)
                ORDER BY "CreatedAtUtc"
                LIMIT @batchSize
                FOR UPDATE SKIP LOCKED
            )
            UPDATE "FailedEmails" AS failed
            SET "ClaimedBy" = @claimedBy,
                "ClaimedAtUtc" = @claimedAtUtc,
                "ClaimedUntilUtc" = @claimedUntilUtc
            FROM claimed
            WHERE failed."Id" = claimed."Id"
            RETURNING failed."Id", failed."To", failed."Subject", failed."HtmlBody",
                      failed."RetryCount", failed."CreatedAtUtc", failed."LastAttemptAtUtc",
                      failed."LastError", failed."TemplateName", failed."IsDeadLettered",
                      failed."ClaimedBy", failed."ClaimedAtUtc", failed."ClaimedUntilUtc";
            """,
            [
                new NpgsqlParameter("cutoff", cutoff),
                new NpgsqlParameter("batchSize", batchSize),
                new NpgsqlParameter("claimedBy", claimedBy),
                new NpgsqlParameter("claimedAtUtc", claimedAtUtc),
                new NpgsqlParameter("claimedUntilUtc", claimedUntilUtc),
            ],
            ct
        );

    public Task UpdateAsync(FailedEmail failedEmail, CancellationToken ct = default)
    {
        _dbContext.FailedEmails.Update(failedEmail);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(FailedEmail failedEmail, CancellationToken ct = default)
    {
        _dbContext.FailedEmails.Remove(failedEmail);
        return Task.CompletedTask;
    }

    private async Task<List<FailedEmail>> ClaimBatchAsync(
        string sql,
        IEnumerable<NpgsqlParameter> parameters,
        CancellationToken ct
    )
    {
        await _dbContext.Database.OpenConnectionAsync(ct);

        try
        {
            await using var command = _dbContext.Database.GetDbConnection().CreateCommand();
            command.CommandText = sql;
            command.Transaction = _dbContext.Database.CurrentTransaction?.GetDbTransaction();

            foreach (var parameter in parameters)
            {
                command.Parameters.Add(parameter);
            }

            var claimedEmails = new List<FailedEmail>();
            await using var reader = await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                claimedEmails.Add(Map(reader));
            }

            return claimedEmails;
        }
        finally
        {
            await _dbContext.Database.CloseConnectionAsync();
        }
    }

    private static FailedEmail Map(System.Data.Common.DbDataReader reader) =>
        new()
        {
            Id = reader.GetGuid(0),
            To = reader.GetString(1),
            Subject = reader.GetString(2),
            HtmlBody = reader.GetString(3),
            RetryCount = reader.GetInt32(4),
            CreatedAtUtc = reader.GetDateTime(5),
            LastAttemptAtUtc = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
            LastError = reader.IsDBNull(7) ? null : reader.GetString(7),
            TemplateName = reader.IsDBNull(8) ? null : reader.GetString(8),
            IsDeadLettered = reader.GetBoolean(9),
            ClaimedBy = reader.IsDBNull(10) ? null : reader.GetString(10),
            ClaimedAtUtc = reader.IsDBNull(11) ? null : reader.GetDateTime(11),
            ClaimedUntilUtc = reader.IsDBNull(12) ? null : reader.GetDateTime(12),
        };
}
