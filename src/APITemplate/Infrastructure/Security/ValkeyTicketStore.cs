using APITemplate.Application.Common.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace APITemplate.Infrastructure.Security;

public sealed class ValkeyTicketStore : ITicketStore
{
    private const string KeyPrefix = "bff:ticket:";

    private readonly IDistributedCache _cache;
    private readonly BffOptions _options;

    public ValkeyTicketStore(IDistributedCache cache, IOptions<BffOptions> options)
    {
        _cache = cache;
        _options = options.Value;
    }

    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var key = KeyPrefix + Guid.NewGuid().ToString("N");
        await RenewAsync(key, ticket);
        return key;
    }

    public async Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        var bytes = TicketSerializer.Default.Serialize(ticket);
        var entryOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.SessionTimeoutMinutes)
        };
        await _cache.SetAsync(key, bytes, entryOptions);
    }

    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        var bytes = await _cache.GetAsync(key);
        return bytes is null ? null : TicketSerializer.Default.Deserialize(bytes);
    }

    public Task RemoveAsync(string key) => _cache.RemoveAsync(key);
}
