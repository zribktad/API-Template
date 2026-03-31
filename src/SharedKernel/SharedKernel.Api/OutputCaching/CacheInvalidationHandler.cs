using SharedKernel.Application.Common.Events;

namespace SharedKernel.Api.OutputCaching;

public static class CacheInvalidationHandler
{
    public static Task HandleAsync(
        CacheInvalidationNotification @event,
        IOutputCacheInvalidationService outputCacheInvalidationService,
        CancellationToken ct
    ) => outputCacheInvalidationService.EvictAsync(@event.CacheTag, ct);
}
