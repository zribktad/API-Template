using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SharedKernel.Application.Context;
using SharedKernel.Application.Security;

namespace SharedKernel.Api.Security;

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
                SharedAuthConstants.Claims.TenantId
            );
            return Guid.TryParse(claimValue, out Guid tenantId) ? tenantId : Guid.Empty;
        }
    }

    public bool HasTenant => TenantId != Guid.Empty;
}
