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
}

public sealed record IdempotencyCacheEntry(
    int StatusCode,
    string? ResponseBody,
    string? ResponseContentType
);
