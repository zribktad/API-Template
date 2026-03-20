namespace APITemplate.Application.Common.Events;

/// <summary>
/// Marker interface for domain events that signal a cache region must be invalidated.
/// </summary>
public interface ICacheInvalidationEvent : IDomainEvent
{
    /// <summary>Gets the cache tag (region key) that should be evicted when this event is published.</summary>
    string CacheTag { get; }
}
