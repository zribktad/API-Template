namespace APITemplate.Application.Common.Contracts;

public interface IIdempotencyStore
{
    Task<IdempotencyCacheEntry?> TryGetAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Atomically checks if the key exists and acquires a lock if not.
    /// Returns true if the lock was acquired (key was not present), false otherwise.
    /// </summary>
    Task<bool> TryAcquireAsync(string key, TimeSpan ttl, CancellationToken ct = default);

    Task SetAsync(
        string key,
        IdempotencyCacheEntry entry,
        TimeSpan ttl,
        CancellationToken ct = default
    );

    /// <summary>
    /// Releases the lock for the given key so a retry with the same key can proceed.
    /// Only releases if the lock is still owned (not yet replaced by a cached result).
    /// </summary>
    Task ReleaseAsync(string key, CancellationToken ct = default);
}

public sealed record IdempotencyCacheEntry(
    int StatusCode,
    string? ResponseBody,
    string? ResponseContentType,
    string? LocationHeader = null
);
