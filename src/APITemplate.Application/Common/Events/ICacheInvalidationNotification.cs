using MediatR;

namespace APITemplate.Application.Common.Events;

/// <summary>
/// Marker interface for MediatR notifications that signal a cache region must be invalidated.
/// Cache-invalidation handlers discover all implementations and evict the appropriate cache entries.
/// </summary>
public interface ICacheInvalidationNotification : INotification
{
    /// <summary>Gets the cache tag (region key) that should be evicted when this notification is published.</summary>
    string CacheTag { get; }
}
