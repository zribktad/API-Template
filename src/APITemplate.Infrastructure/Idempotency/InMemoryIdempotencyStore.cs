using System.Collections.Concurrent;
using System.Text.Json;
using APITemplate.Application.Common.Contracts;

namespace APITemplate.Infrastructure.Idempotency;

public sealed class InMemoryIdempotencyStore : IIdempotencyStore
{
    private const string LockSuffix = ":lock";
    private const string LockValue = "processing";

    private readonly ConcurrentDictionary<string, (string Value, DateTimeOffset Expiry)> _store =
        new();
    private readonly TimeProvider _timeProvider;

    public InMemoryIdempotencyStore(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public Task<IdempotencyCacheEntry?> TryGetAsync(string key, CancellationToken ct = default)
    {
        if (_store.TryGetValue(key, out var entry) && entry.Expiry > _timeProvider.GetUtcNow())
        {
            var result = JsonSerializer.Deserialize<IdempotencyCacheEntry>(entry.Value);
            return Task.FromResult(result);
        }

        EvictExpired();
        return Task.FromResult<IdempotencyCacheEntry?>(null);
    }

    public Task<bool> TryAcquireAsync(string key, TimeSpan ttl, CancellationToken ct = default)
    {
        EvictExpired();

        var lockKey = key + LockSuffix;
        var expiry = _timeProvider.GetUtcNow().Add(ttl);
        var acquired = _store.TryAdd(lockKey, (LockValue, expiry));
        return Task.FromResult(acquired);
    }

    public Task SetAsync(
        string key,
        IdempotencyCacheEntry entry,
        TimeSpan ttl,
        CancellationToken ct = default
    )
    {
        var json = JsonSerializer.Serialize(entry);
        var expiry = _timeProvider.GetUtcNow().Add(ttl);
        _store[key] = (json, expiry);
        return Task.CompletedTask;
    }

    public Task ReleaseAsync(string key, CancellationToken ct = default)
    {
        var lockKey = key + LockSuffix;
        _store.TryRemove(lockKey, out _);
        return Task.CompletedTask;
    }

    private void EvictExpired()
    {
        var now = _timeProvider.GetUtcNow();
        foreach (var kvp in _store)
        {
            if (kvp.Value.Expiry <= now)
                _store.TryRemove(kvp);
        }
    }
}
