using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using SharedKernel.Application.Security;

namespace SharedKernel.Api.OutputCaching;

/// <summary>
/// Allows caching for authenticated GET/HEAD requests and isolates cache entries by tenant.
/// </summary>
public sealed class TenantAwareOutputCachePolicy : IOutputCachePolicy
{
    private readonly string? _tag;
    private readonly TimeSpan? _expiration;

    public TenantAwareOutputCachePolicy()
        : this(null, null) { }

    public TenantAwareOutputCachePolicy(string? tag, TimeSpan? expiration)
    {
        _tag = tag;
        _expiration = expiration;
    }

    public ValueTask CacheRequestAsync(
        OutputCacheContext context,
        CancellationToken cancellationToken
    )
    {
        if (
            !HttpMethods.IsGet(context.HttpContext.Request.Method)
            && !HttpMethods.IsHead(context.HttpContext.Request.Method)
        )
        {
            return ValueTask.CompletedTask;
        }

        context.EnableOutputCaching = true;
        context.AllowCacheLookup = true;
        context.AllowCacheStorage = true;

        string tenantId =
            context.HttpContext.User.FindFirstValue(SharedAuthConstants.Claims.TenantId)
            ?? string.Empty;
        context.CacheVaryByRules.VaryByValues[SharedAuthConstants.Claims.TenantId] = tenantId;
        if (!string.IsNullOrWhiteSpace(_tag))
        {
            context.Tags.Add(_tag);
        }

        if (_expiration.HasValue)
        {
            context.ResponseExpirationTimeSpan = _expiration;
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask ServeFromCacheAsync(
        OutputCacheContext context,
        CancellationToken cancellationToken
    ) => ValueTask.CompletedTask;

    public ValueTask ServeResponseAsync(
        OutputCacheContext context,
        CancellationToken cancellationToken
    ) => ValueTask.CompletedTask;
}
