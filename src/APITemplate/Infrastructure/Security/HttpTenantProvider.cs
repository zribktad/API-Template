using System.Security.Claims;
using APITemplate.Application.Common.Context;
using APITemplate.Application.Common.Security;

namespace APITemplate.Infrastructure.Security;

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
            var claimValue = _httpContextAccessor.HttpContext?.User.FindFirstValue(CustomClaimTypes.TenantId);
            return Guid.TryParse(claimValue, out var tenantId) ? tenantId : Guid.Empty;
        }
    }

    public bool HasTenant => TenantId != Guid.Empty;
}
