using System.Security.Claims;
using Identity.Application.Options;
using Identity.Application.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SharedKernel.Application.Context;

namespace Identity.Infrastructure.Security;

/// <summary>
/// Resolves actor identity for auditing from the current HTTP principal.
/// </summary>
public sealed class HttpActorProvider : IActorProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly SystemIdentityOptions _systemIdentity;

    public HttpActorProvider(
        IHttpContextAccessor httpContextAccessor,
        IOptions<SystemIdentityOptions> systemIdentityOptions
    )
    {
        _httpContextAccessor = httpContextAccessor;
        _systemIdentity = systemIdentityOptions.Value;
    }

    public Guid ActorId
    {
        get
        {
            ClaimsPrincipal? user = _httpContextAccessor.HttpContext?.User;
            string? raw =
                user?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user?.FindFirstValue(AuthConstants.Claims.Subject)
                ?? user?.FindFirstValue(ClaimTypes.Name);

            return Guid.TryParse(raw, out Guid id) ? id : _systemIdentity.DefaultActorId;
        }
    }
}
