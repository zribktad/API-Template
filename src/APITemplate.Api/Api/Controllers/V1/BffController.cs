using System.Security.Claims;
using APITemplate.Api.Controllers;
using APITemplate.Application.Common.Options;
using APITemplate.Application.Common.Security;
using APITemplate.Application.Features.Bff.DTOs;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace APITemplate.Api.Controllers.V1;

[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/bff")]
[Authorize(AuthenticationSchemes = AuthConstants.BffSchemes.Cookie)]
public sealed class BffController : ApiControllerBase
{
    private readonly BffOptions _bffOptions;

    public BffController(IOptions<BffOptions> bffOptions)
    {
        _bffOptions = bffOptions.Value;
    }

    [HttpGet("login")]
    [AllowAnonymous]
    public IActionResult Login([FromQuery] string? returnUrl = null)
    {
        var redirectUri = Url.IsLocalUrl(returnUrl) ? returnUrl : "/";
        return Challenge(
            new AuthenticationProperties { RedirectUri = redirectUri },
            AuthConstants.BffSchemes.Oidc
        );
    }

    [HttpGet("logout")]
    public IActionResult Logout()
    {
        return SignOut(
            new AuthenticationProperties { RedirectUri = _bffOptions.PostLogoutRedirectUri },
            AuthConstants.BffSchemes.Cookie,
            AuthConstants.BffSchemes.Oidc
        );
    }

    [HttpGet("csrf")]
    [AllowAnonymous]
    public IActionResult GetCsrf() =>
        Ok(
            new
            {
                headerName = AuthConstants.Csrf.HeaderName,
                headerValue = AuthConstants.Csrf.HeaderValue,
            }
        );

    [HttpGet("user")]
    public IActionResult GetUser()
    {
        var user = HttpContext.User;

        var result = new BffUserResponse(
            UserId: user.FindFirstValue(ClaimTypes.NameIdentifier),
            Username: user.FindFirstValue(ClaimTypes.Name),
            Email: user.FindFirstValue(ClaimTypes.Email),
            TenantId: user.FindFirstValue(AuthConstants.Claims.TenantId),
            Roles: user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray()
        );

        return Ok(result);
    }
}
