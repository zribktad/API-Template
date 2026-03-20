using APITemplate.Application.Common.Events;

namespace APITemplate.Api.Cache;

/// <summary>
/// Generic domain event handler that bridges domain events implementing
/// <see cref="ICacheInvalidationEvent"/> to the output cache invalidation pipeline.
/// </summary>
public sealed class CacheInvalidationHandler<TEvent> : IDomainEventHandler<TEvent>
    where TEvent : ICacheInvalidationEvent
{
    private readonly IOutputCacheInvalidationService _outputCacheInvalidationService;

    public CacheInvalidationHandler(IOutputCacheInvalidationService outputCacheInvalidationService)
    {
        _outputCacheInvalidationService = outputCacheInvalidationService;
    }

    /// <summary>
    /// Evicts the output cache entries tagged with the value provided by the event.
    /// </summary>
    public Task HandleAsync(TEvent @event, CancellationToken cancellationToken) =>
        _outputCacheInvalidationService.EvictAsync(@event.CacheTag, cancellationToken);
}
