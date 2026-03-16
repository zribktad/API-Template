using APITemplate.Application.Common.BackgroundJobs;
using APITemplate.Application.Common.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TickerQ.Utilities.Entities;

namespace APITemplate.Infrastructure.BackgroundJobs.TickerQ;

public sealed class TickerQRecurringJobRegistrar
{
    private const string SeedIdentifier = "APITemplate:TickerQ:Recurring";
    private const string InitIdentifierProperty = "InitIdentifier";
    private const string CreatedAtProperty = "CreatedAt";
    private const string UpdatedAtProperty = "UpdatedAt";

    private readonly TickerQSchedulerDbContext _dbContext;
    private readonly IEnumerable<IRecurringBackgroundJobRegistration> _registrations;
    private readonly BackgroundJobsOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<TickerQRecurringJobRegistrar> _logger;

    public TickerQRecurringJobRegistrar(
        TickerQSchedulerDbContext dbContext,
        IEnumerable<IRecurringBackgroundJobRegistration> registrations,
        IOptions<BackgroundJobsOptions> options,
        TimeProvider timeProvider,
        ILogger<TickerQRecurringJobRegistrar> logger
    )
    {
        _dbContext = dbContext;
        _registrations = registrations;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task SyncAsync(CancellationToken ct = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var definitions = _registrations.Select(x => x.Build(_options)).ToList();
        var tickers = await _dbContext.Set<CronTickerEntity>().ToListAsync(ct);

        foreach (var definition in definitions)
        {
            var existing = tickers.SingleOrDefault(x => x.Id == definition.Id);
            if (existing is null)
            {
                var entity = new CronTickerEntity
                {
                    Id = definition.Id,
                    Function = definition.FunctionName,
                    Description = definition.Description,
                    Expression = definition.CronExpression,
                    IsEnabled = definition.Enabled,
                    Retries = definition.Retries,
                    RetryIntervals = definition.RetryIntervals ?? [],
                };
                _dbContext.Set<CronTickerEntity>().Add(entity);
                var entry = _dbContext.Entry(entity);
                StampMetadata(entry, now);
                continue;
            }

            existing.Function = definition.FunctionName;
            existing.Description = definition.Description;
            existing.Expression = definition.CronExpression;
            existing.IsEnabled = definition.Enabled;
            existing.Retries = definition.Retries;
            existing.RetryIntervals = definition.RetryIntervals ?? [];
            StampUpdatedMetadata(_dbContext.Entry(existing), now);
        }

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Synchronized {Count} recurring TickerQ job definitions.",
            definitions.Count
        );
    }

    private static void StampMetadata(
        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<CronTickerEntity> entry,
        DateTime now
    )
    {
        entry.Property(InitIdentifierProperty).CurrentValue = SeedIdentifier;
        entry.Property(CreatedAtProperty).CurrentValue = now;
        entry.Property(UpdatedAtProperty).CurrentValue = now;
    }

    private static void StampUpdatedMetadata(
        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<CronTickerEntity> entry,
        DateTime now
    )
    {
        entry.Property(InitIdentifierProperty).CurrentValue ??= SeedIdentifier;
        entry.Property(UpdatedAtProperty).CurrentValue = now;
    }
}
