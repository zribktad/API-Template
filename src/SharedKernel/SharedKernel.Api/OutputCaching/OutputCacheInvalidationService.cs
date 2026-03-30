using System.Diagnostics;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Logging;
using SharedKernel.Infrastructure.Observability;

namespace SharedKernel.Api.OutputCaching;

public sealed class OutputCacheInvalidationService : IOutputCacheInvalidationService
{
    private readonly IOutputCacheStore _store;
    private readonly ILogger<OutputCacheInvalidationService> _logger;

    public OutputCacheInvalidationService(
        IOutputCacheStore store,
        ILogger<OutputCacheInvalidationService> logger
    )
    {
        _store = store;
        _logger = logger;
    }

    public Task EvictAsync(string tag, CancellationToken cancellationToken = default) =>
        EvictAsync([tag], cancellationToken);

    public async Task EvictAsync(
        IEnumerable<string> tags,
        CancellationToken cancellationToken = default
    )
    {
        foreach (string tag in tags.Distinct(StringComparer.Ordinal))
        {
            long startedAt = Stopwatch.GetTimestamp();
            try
            {
                using Activity? activity = CacheTelemetry.StartOutputCacheInvalidationActivity(tag);
                await _store.EvictByTagAsync(tag, cancellationToken);
                CacheTelemetry.RecordOutputCacheInvalidation(
                    tag,
                    Stopwatch.GetElapsedTime(startedAt)
                );
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed to evict output cache tag {Tag}.", tag);
            }
        }
    }
}
