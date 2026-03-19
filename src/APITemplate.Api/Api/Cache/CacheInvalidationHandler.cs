using APITemplate.Application.Common.Events;
using MediatR;

namespace APITemplate.Api.Cache;

/// <summary>
/// Generic MediatR notification handler that bridges domain events implementing
/// <see cref="ICacheInvalidationNotification"/> to the output cache invalidation pipeline.
/// </summary>
public sealed class CacheInvalidationHandler<TNotification> : INotificationHandler<TNotification>
    where TNotification : ICacheInvalidationNotification
{
    private readonly IOutputCacheInvalidationService _outputCacheInvalidationService;

    public CacheInvalidationHandler(IOutputCacheInvalidationService outputCacheInvalidationService)
    {
        _outputCacheInvalidationService = outputCacheInvalidationService;
    }

    /// <summary>
    /// Evicts the output cache entries tagged with the value provided by the notification.
    /// </summary>
    public Task Handle(TNotification notification, CancellationToken cancellationToken) =>
        _outputCacheInvalidationService.EvictAsync(notification.CacheTag, cancellationToken);
}
