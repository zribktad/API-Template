using System.Security.Claims;
using FileStorage.Application.Security;
using Microsoft.AspNetCore.Http;
using SharedKernel.Application.Context;

namespace FileStorage.Infrastructure.Security;

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
                ?? user?.FindFirstValue(AuthConstants.Claims.Subject)
                ?? user?.FindFirstValue(ClaimTypes.Name);

            return Guid.TryParse(raw, out Guid id) ? id : Guid.Empty;
        }
    }
}
