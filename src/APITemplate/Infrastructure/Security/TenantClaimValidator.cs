using System.Security.Claims;
using APITemplate.Application.Common.Security;

namespace APITemplate.Infrastructure.Security;

public static class TenantClaimValidator
{
    public static bool HasValidTenantClaim(ClaimsPrincipal? principal)
        => principal?.HasClaim(
            c => c.Type == CustomClaimTypes.TenantId
                 && Guid.TryParse(c.Value, out var id)
                 && id != Guid.Empty) == true;
}
