using APITemplate.Application.Common.BackgroundJobs;
using APITemplate.Domain.Entities;
using APITemplate.Domain.Enums;
using APITemplate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace APITemplate.Infrastructure.BackgroundJobs.Services;

public sealed class CleanupService : ICleanupService
{
    private readonly AppDbContext _dbContext;
    private readonly MongoDbContext? _mongoDbContext;
    private readonly IEnumerable<ISoftDeleteCleanupStrategy> _cleanupStrategies;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<CleanupService> _logger;

    public CleanupService(
        AppDbContext dbContext,
        IEnumerable<ISoftDeleteCleanupStrategy> cleanupStrategies,
        TimeProvider timeProvider,
        ILogger<CleanupService> logger,
        MongoDbContext? mongoDbContext = null
    )
    {
        _dbContext = dbContext;
        _cleanupStrategies = cleanupStrategies;
        _timeProvider = timeProvider;
        _mongoDbContext = mongoDbContext;
        _logger = logger;
    }

    public async Task CleanupExpiredInvitationsAsync(
        int retentionHours,
        int batchSize,
        CancellationToken ct = default
    )
    {
        var cutoff = _timeProvider.GetUtcNow().UtcDateTime.AddHours(-retentionHours);
        int totalDeleted = 0;
        int deleted;

        do
        {
            deleted = await _dbContext
                .TenantInvitations.IgnoreQueryFilters()
                .Where(i => i.Status == InvitationStatus.Pending && i.ExpiresAtUtc < cutoff)
                .OrderBy(i => i.ExpiresAtUtc)
                .Take(batchSize)
                .ExecuteDeleteAsync(ct);

            totalDeleted += deleted;
        } while (deleted == batchSize);

        if (totalDeleted > 0)
        {
            _logger.LogInformation("Cleaned up {Count} expired invitations.", totalDeleted);
        }
    }

    public async Task CleanupSoftDeletedRecordsAsync(
        int retentionDays,
        int batchSize,
        CancellationToken ct = default
    )
    {
        var cutoff = _timeProvider.GetUtcNow().UtcDateTime.AddDays(-retentionDays);

        foreach (var strategy in _cleanupStrategies)
        {
            var deleted = await strategy.CleanupAsync(cutoff, batchSize, ct);

            if (deleted > 0)
            {
                _logger.LogInformation(
                    "Cleaned up {Count} soft-deleted records from {Entity}.",
                    deleted,
                    strategy.EntityName
                );
            }
        }
    }

    /// <summary>
    /// Safety net for orphaned MongoDB ProductData documents that are no longer linked
    /// from any ProductDataLink in PostgreSQL. Under normal operation, cascade rules
    /// (ProductSoftDeleteCascadeRule, ProductDataCascadeDeleteHandler) handle cleanup.
    /// Orphans may appear after transaction failures, manual DB edits, or cascade bugs.
    /// </summary>
    public async Task CleanupOrphanedProductDataAsync(
        int retentionDays,
        int batchSize,
        CancellationToken ct = default
    )
    {
        if (_mongoDbContext is null)
        {
            _logger.LogDebug(
                "MongoDbContext not available, skipping orphaned product data cleanup."
            );
            return;
        }

        var mongoCollection = _mongoDbContext.ProductData;
        var cutoff = _timeProvider.GetUtcNow().UtcDateTime.AddDays(-retentionDays);

        var linkedIds = await _dbContext
            .ProductDataLinks.IgnoreQueryFilters()
            .Select(l => l.ProductDataId)
            .Distinct()
            .ToListAsync(ct);

        var linkedIdSet = new HashSet<Guid>(linkedIds);

        // Only consider documents older than retention cutoff — gives cascade rules time to complete
        var filter = Builders<ProductData>.Filter.Lt(d => d.CreatedAt, cutoff);
        var allDocs = await mongoCollection.Find(filter).Project(d => d.Id).ToListAsync(ct);

        var orphanedIds = allDocs.Where(id => !linkedIdSet.Contains(id)).ToList();

        if (orphanedIds.Count == 0)
        {
            return;
        }

        foreach (var batch in orphanedIds.Chunk(batchSize))
        {
            var deleteFilter = Builders<ProductData>.Filter.In(d => d.Id, batch);
            await mongoCollection.DeleteManyAsync(deleteFilter, ct);
        }

        _logger.LogInformation(
            "Cleaned up {Count} orphaned product data documents.",
            orphanedIds.Count
        );
    }
}
