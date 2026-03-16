using System.Text.RegularExpressions;
using APITemplate.Application.Common.BackgroundJobs;
using APITemplate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace APITemplate.Infrastructure.BackgroundJobs.Services;

public sealed partial class ReindexService : IReindexService
{
    private const double BloatThresholdPercent = 30.0;

    private readonly AppDbContext _dbContext;
    private readonly ILogger<ReindexService> _logger;

    public ReindexService(AppDbContext dbContext, ILogger<ReindexService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Safety net for FTS index bloat after heavy write activity. PostgreSQL autovacuum
    /// handles routine maintenance, but cannot reclaim index bloat — only REINDEX can.
    /// This method checks actual bloat ratio before reindexing to avoid unnecessary work.
    /// Scoped to the current database's public schema to avoid touching other schemas.
    /// </summary>
    public async Task ReindexFullTextSearchAsync(CancellationToken ct = default)
    {
        // pg_indexes is a PostgreSQL system catalog — no EF Core model exists for it.
        // REINDEX INDEX CONCURRENTLY is DDL — no EF Core API exists for it.
        var ftsIndexes = await _dbContext
            .Database.SqlQueryRaw<string>(
                """
                SELECT indexname AS "Value"
                FROM pg_indexes
                WHERE schemaname = 'public'
                  AND indexdef LIKE '%to_tsvector%'
                """
            )
            .ToListAsync(ct);

        foreach (var index in ftsIndexes)
        {
            if (!ValidIndexNameRegex().IsMatch(index))
            {
                _logger.LogWarning("Skipping invalid FTS index name: {IndexName}.", index);
                continue;
            }

            var bloatPercent = await GetIndexBloatPercentAsync(index, ct);

            if (bloatPercent < BloatThresholdPercent)
            {
                _logger.LogDebug(
                    "FTS index {IndexName} bloat {BloatPercent:F1}% is below threshold {Threshold}%, skipping.",
                    index,
                    bloatPercent,
                    BloatThresholdPercent
                );
                continue;
            }

            _logger.LogInformation(
                "FTS index {IndexName} bloat {BloatPercent:F1}% exceeds threshold {Threshold}%, reindexing.",
                index,
                bloatPercent,
                BloatThresholdPercent
            );

            await _dbContext.Database.ExecuteSqlRawAsync(
                $"REINDEX INDEX CONCURRENTLY \"{index}\"",
                ct
            );

            _logger.LogInformation("Reindexed FTS index {IndexName}.", index);
        }
    }

    private async Task<double> GetIndexBloatPercentAsync(string indexName, CancellationToken ct)
    {
        // Compare actual index size to estimated "ideal" size based on live tuples.
        // pg_relation_size = actual bytes on disk, pg_stat_user_indexes = live tuple count.
        // A significant gap indicates bloat from dead tuples that autovacuum cannot reclaim.
        var result = await _dbContext
            .Database.SqlQueryRaw<double>(
                """
                SELECT CASE
                    WHEN pg_relation_size(i.indexrelid) = 0 THEN 0
                    ELSE GREATEST(0,
                        100.0 * (1.0 - (s.idx_tup_read::float / NULLIF(pg_relation_size(i.indexrelid) / 8192.0, 0)))
                    )
                END AS "Value"
                FROM pg_stat_user_indexes s
                JOIN pg_index i ON s.indexrelid = i.indexrelid
                WHERE s.indexrelname = {0}
                """,
                indexName
            )
            .ToListAsync(ct);

        return result.FirstOrDefault();
    }

    [GeneratedRegex(@"^[a-zA-Z_][a-zA-Z0-9_]*$")]
    private static partial Regex ValidIndexNameRegex();
}
