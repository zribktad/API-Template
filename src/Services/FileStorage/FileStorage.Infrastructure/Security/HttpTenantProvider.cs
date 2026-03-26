using System.Security.Claims;
using FileStorage.Application.Security;
using Microsoft.AspNetCore.Http;
using SharedKernel.Application.Context;

namespace FileStorage.Infrastructure.Security;

/// <summary>
/// Resolves tenant identity from the current authenticated HTTP principal.
/// </summary>
public sealed class HttpTenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpTenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid TenantId
    {
        get
        {
            string? claimValue = _httpContextAccessor.HttpContext?.User.FindFirstValue(
                AuthConstants.Claims.TenantId
            );
            return Guid.TryParse(claimValue, out Guid tenantId) ? tenantId : Guid.Empty;
        }
    }

    public bool HasTenant => TenantId != Guid.Empty;
}
