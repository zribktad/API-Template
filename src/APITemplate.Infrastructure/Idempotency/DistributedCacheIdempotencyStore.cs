using System.Text.Json;
using APITemplate.Application.Common.Contracts;
using StackExchange.Redis;

namespace APITemplate.Infrastructure.Idempotency;

public sealed class DistributedCacheIdempotencyStore : IIdempotencyStore
{
    private const string KeyPrefix = "idempotency:";

    private static readonly LuaScript ReleaseLockScript = LuaScript.Prepare(
        "if redis.call('get', @key) == @value then return redis.call('del', @key) else return 0 end"
    );

    private readonly IDatabase _database;

    public DistributedCacheIdempotencyStore(IConnectionMultiplexer connectionMultiplexer)
    {
        _database = connectionMultiplexer.GetDatabase();
    }

    public async Task<IdempotencyCacheEntry?> TryGetAsync(
        string key,
        CancellationToken ct = default
    )
    {
        var json = await _database.StringGetAsync(KeyPrefix + key);
        return json.IsNullOrEmpty
            ? null
            : JsonSerializer.Deserialize<IdempotencyCacheEntry>(json.ToString());
    }

    public async Task<bool> TryAcquireAsync(
        string key,
        TimeSpan ttl,
        CancellationToken ct = default
    )
    {
        var lockKey = KeyPrefix + key + IdempotencyStoreConstants.LockSuffix;
        return await _database.StringSetAsync(
            lockKey,
            IdempotencyStoreConstants.LockValue,
            ttl,
            when: When.NotExists
        );
    }

    public async Task SetAsync(
        string key,
        IdempotencyCacheEntry entry,
        TimeSpan ttl,
        CancellationToken ct = default
    )
    {
        var json = JsonSerializer.Serialize(entry);
        await _database.StringSetAsync(KeyPrefix + key, json, ttl);
    }

    public async Task ReleaseAsync(string key, CancellationToken ct = default)
    {
        var lockKey = KeyPrefix + key + IdempotencyStoreConstants.LockSuffix;
        await _database.ScriptEvaluateAsync(
            ReleaseLockScript,
            new { key = (RedisKey)lockKey, value = (RedisValue)IdempotencyStoreConstants.LockValue }
        );
    }
}
