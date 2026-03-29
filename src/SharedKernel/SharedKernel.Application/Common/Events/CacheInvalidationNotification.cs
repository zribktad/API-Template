namespace SharedKernel.Application.Common.Events;

/// <summary>
/// Message used to evict one named output-cache tag.
/// </summary>
public sealed record CacheInvalidationNotification(string CacheTag);
