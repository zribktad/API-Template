using APITemplate.Application.Common.Events;
using MediatR;

namespace APITemplate.Api.Cache;

public sealed class CacheInvalidationHandler<TNotification> : INotificationHandler<TNotification>
    where TNotification : ICacheInvalidationNotification
{
    private readonly IOutputCacheInvalidationService _outputCacheInvalidationService;

    public CacheInvalidationHandler(IOutputCacheInvalidationService outputCacheInvalidationService)
    {
        _outputCacheInvalidationService = outputCacheInvalidationService;
    }

    public Task Handle(TNotification notification, CancellationToken cancellationToken) =>
        _outputCacheInvalidationService.EvictAsync(notification.CacheTag, cancellationToken);
}
