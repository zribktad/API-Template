using System.Security.Cryptography;
using Identity.Application.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Identity.Infrastructure.Security.Bff;

/// <summary>
/// Stores authentication tickets in a distributed cache (Dragonfly/Valkey) instead of embedding
/// them in the cookie. This keeps cookies small and enables server-side session revocation.
/// </summary>
public sealed class ValkeyTicketStore : ITicketStore
{
    private const string KeyPrefix = "bff:ticket:";
    private const string ProtectorPurpose = "bff:ticket";

    private readonly IDistributedCache _cache;
    private readonly IDataProtector _protector;
    private readonly BffOptions _bffOptions;

    public ValkeyTicketStore(
        IDistributedCache cache,
        IDataProtectionProvider dataProtectionProvider,
        IOptions<BffOptions> bffOptions
    )
    {
        _cache = cache;
        _protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);
        _bffOptions = bffOptions.Value;
    }

    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        string key = Guid.NewGuid().ToString("N");
        await SetTicketAsync(key, ticket);
        return key;
    }

    public async Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        await SetTicketAsync(key, ticket);
    }

    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        byte[]? bytes = await _cache.GetAsync(KeyPrefix + key);

        if (bytes is null)
            return null;

        try
        {
            byte[] unprotected = _protector.Unprotect(bytes);
            return TicketSerializer.Default.Deserialize(unprotected);
        }
        catch (CryptographicException)
        {
            return null;
        }
    }

    public async Task RemoveAsync(string key)
    {
        await _cache.RemoveAsync(KeyPrefix + key);
    }

    private async Task SetTicketAsync(string key, AuthenticationTicket ticket)
    {
        byte[] serialized = TicketSerializer.Default.Serialize(ticket);
        byte[] protectedBytes = _protector.Protect(serialized);

        DistributedCacheEntryOptions cacheOptions = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(
                _bffOptions.SessionTimeoutMinutes
            ),
        };

        await _cache.SetAsync(KeyPrefix + key, protectedBytes, cacheOptions);
    }
}
