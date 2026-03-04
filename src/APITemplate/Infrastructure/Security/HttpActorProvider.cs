using System.Security.Claims;
using APITemplate.Application.Common.Context;
using APITemplate.Application.Common.Options;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;

namespace APITemplate.Infrastructure.Security;

public sealed class HttpActorProvider : IActorProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly SystemIdentityOptions _systemIdentity;

    public HttpActorProvider(
        IHttpContextAccessor httpContextAccessor,
        IOptions<SystemIdentityOptions> systemIdentityOptions)
    {
        _httpContextAccessor = httpContextAccessor;
        _systemIdentity = systemIdentityOptions.Value;
    }

    public string ActorId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? user?.FindFirstValue("sub")
                ?? user?.FindFirstValue(ClaimTypes.Name)
                ?? _systemIdentity.DefaultActorId;
        }
    }
}
