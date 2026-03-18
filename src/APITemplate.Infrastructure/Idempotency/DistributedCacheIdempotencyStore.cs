using System.Text.Json;
using APITemplate.Application.Common.Contracts;
using Microsoft.Extensions.Caching.Distributed;

namespace APITemplate.Infrastructure.Idempotency;

public sealed class DistributedCacheIdempotencyStore : IIdempotencyStore
{
    private readonly IDistributedCache _cache;
    private const string KeyPrefix = "idempotency:";
    private const string LockSuffix = ":lock";

    public DistributedCacheIdempotencyStore(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<IdempotencyCacheEntry?> TryGetAsync(
        string key,
        CancellationToken ct = default
    )
    {
        var json = await _cache.GetStringAsync(KeyPrefix + key, ct);
        return json is null ? null : JsonSerializer.Deserialize<IdempotencyCacheEntry>(json);
    }

    public async Task<bool> TryAcquireAsync(
        string key,
        TimeSpan ttl,
        CancellationToken ct = default
    )
    {
        var lockKey = KeyPrefix + key + LockSuffix;
        var existing = await _cache.GetStringAsync(lockKey, ct);
        if (existing is not null)
            return false;

        await _cache.SetStringAsync(
            lockKey,
            "processing",
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
            ct
        );
        return true;
    }

    public async Task SetAsync(
        string key,
        IdempotencyCacheEntry entry,
        TimeSpan ttl,
        CancellationToken ct = default
    )
    {
        var json = JsonSerializer.Serialize(entry);
        await _cache.SetStringAsync(
            KeyPrefix + key,
            json,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
            ct
        );
    }
}
