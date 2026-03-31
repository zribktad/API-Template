using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SharedKernel.Application.Context;
using SharedKernel.Application.Security;

namespace SharedKernel.Api.Security;

/// <summary>
/// Resolves actor identity for auditing from the current HTTP principal.
/// </summary>
public sealed class HttpActorProvider : IActorProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpActorProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid ActorId
    {
        get
        {
            ClaimsPrincipal? user = _httpContextAccessor.HttpContext?.User;
            string? raw =
                user?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user?.FindFirstValue(SharedAuthConstants.Claims.Subject)
                ?? user?.FindFirstValue(ClaimTypes.Name);

            return Guid.TryParse(raw, out Guid id) ? id : Guid.Empty;
        }
    }
}
