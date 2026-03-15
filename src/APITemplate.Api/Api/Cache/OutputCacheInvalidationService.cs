using System.Diagnostics;
using APITemplate.Infrastructure.Observability;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Logging;

namespace APITemplate.Api.Cache;

public sealed class OutputCacheInvalidationService : IOutputCacheInvalidationService
{
    private readonly IOutputCacheStore _outputCacheStore;
    private readonly ILogger<OutputCacheInvalidationService> _logger;

    public OutputCacheInvalidationService(
        IOutputCacheStore outputCacheStore,
        ILogger<OutputCacheInvalidationService> logger
    )
    {
        _outputCacheStore = outputCacheStore;
        _logger = logger;
    }

    public Task EvictAsync(string tag, CancellationToken cancellationToken = default) =>
        EvictAsync([tag], cancellationToken);

    public async Task EvictAsync(
        IEnumerable<string> tags,
        CancellationToken cancellationToken = default
    )
    {
        foreach (var tag in tags.Distinct(StringComparer.Ordinal))
        {
            var startedAt = Stopwatch.GetTimestamp();
            using var activity = CacheTelemetry.StartOutputCacheInvalidationActivity(tag);

            try
            {
                await _outputCacheStore.EvictByTagAsync(tag, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to evict output cache for tag {Tag}. Stale data may be served until expiration.",
                    tag
                );
                continue;
            }

            CacheTelemetry.RecordOutputCacheInvalidation(tag, Stopwatch.GetElapsedTime(startedAt));
        }
    }
}
