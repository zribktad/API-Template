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
        // Estimates index bloat by comparing actual index size (pg_relation_size) to an ideal
        // size derived from live table rows and average tuple width. The ideal size assumes
        // ~90% fillfactor (0.9) and one pointer per tuple (6 bytes header + avg_width).
        // This is a lightweight heuristic — for precise measurements use pgstattuple extension.
        var result = await _dbContext
            .Database.SqlQueryRaw<double>(
                """
                SELECT CASE
                    WHEN pg_relation_size(c.oid) = 0 THEN 0
                    ELSE GREATEST(0,
                        100.0 * (1.0 - (
                            (s.n_live_tup::float * COALESCE(NULLIF(s.avg_width, 0), 32) / 0.9)
                            / NULLIF(pg_relation_size(c.oid)::float, 0)
                        ))
                    )
                END AS "Value"
                FROM pg_class c
                JOIN pg_stat_user_indexes si ON si.indexrelid = c.oid
                JOIN pg_stat_user_tables s ON s.relid = si.relid
                WHERE c.relname = {0}
                """,
                indexName
            )
            .ToListAsync(ct);

        return result.FirstOrDefault();
    }

    [GeneratedRegex(@"^[a-zA-Z_][a-zA-Z0-9_]*$")]
    private static partial Regex ValidIndexNameRegex();
}
