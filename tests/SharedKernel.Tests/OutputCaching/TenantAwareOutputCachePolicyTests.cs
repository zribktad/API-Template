using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using SharedKernel.Api.OutputCaching;
using SharedKernel.Application.Security;
using Shouldly;
using Xunit;

namespace SharedKernel.Tests.OutputCaching;

public sealed class TenantAwareOutputCachePolicyTests
{
    [Fact]
    public async Task CacheRequestAsync_GetRequest_EnablesCacheAndVariesByTenant()
    {
        var sut = new TenantAwareOutputCachePolicy();
        var context = CreateContext("GET", Guid.NewGuid());

        await sut.CacheRequestAsync(context, TestContext.Current.CancellationToken);

        context.EnableOutputCaching.ShouldBeTrue();
        context.AllowCacheLookup.ShouldBeTrue();
        context.AllowCacheStorage.ShouldBeTrue();
        context
            .CacheVaryByRules.VaryByValues.ContainsKey(SharedAuthConstants.Claims.TenantId)
            .ShouldBeTrue();
    }

    [Fact]
    public async Task CacheRequestAsync_HeadRequest_EnablesCache()
    {
        var sut = new TenantAwareOutputCachePolicy();
        var context = CreateContext("HEAD", Guid.NewGuid());

        await sut.CacheRequestAsync(context, TestContext.Current.CancellationToken);

        context.EnableOutputCaching.ShouldBeTrue();
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [InlineData("PATCH")]
    public async Task CacheRequestAsync_NonReadRequest_DoesNotEnableCache(string method)
    {
        var sut = new TenantAwareOutputCachePolicy();
        var context = CreateContext(method, Guid.NewGuid());

        await sut.CacheRequestAsync(context, TestContext.Current.CancellationToken);

        context.EnableOutputCaching.ShouldBeFalse();
        context.AllowCacheLookup.ShouldBeFalse();
        context.AllowCacheStorage.ShouldBeFalse();
    }

    private static OutputCacheContext CreateContext(string method, Guid tenantId)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = method;
        httpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity(
                [new Claim(SharedAuthConstants.Claims.TenantId, tenantId.ToString())],
                "Test"
            )
        );

        return new OutputCacheContext { HttpContext = httpContext };
    }
}
