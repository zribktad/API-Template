using System.IdentityModel.Tokens.Jwt;
using APITemplate.Application.Common.Security;
using APITemplate.Application.Features.Bff.DTOs;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APITemplate.Api.Controllers.V1;

[ApiController]
[Route("bff")]
public sealed class BffController : ControllerBase
{
    [HttpGet("login")]
    [AllowAnonymous]
    public IActionResult Login([FromQuery] string? returnUrl = "/")
    {
        if (string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl))
            returnUrl = "/";

        var properties = new AuthenticationProperties { RedirectUri = returnUrl };
        return Challenge(properties, BffAuthenticationSchemes.Oidc);
    }

    [HttpPost("logout")]
    [Authorize(AuthenticationSchemes = BffAuthenticationSchemes.Cookie)]
    public async Task<IActionResult> Logout([FromServices] IAntiforgery antiforgery)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return BadRequest("Invalid antiforgery token.");
        }

        return SignOut(
            new AuthenticationProperties { RedirectUri = "/" },
            BffAuthenticationSchemes.Cookie,
            BffAuthenticationSchemes.Oidc);
    }

    [HttpGet("user")]
    [Authorize(AuthenticationSchemes = BffAuthenticationSchemes.Cookie)]
    public IActionResult GetUser([FromServices] IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        Response.Headers["X-XSRF-TOKEN"] = tokens.RequestToken;

        var user = new BffUserResponse(
            Sub: User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? "",
            PreferredUsername: User.FindFirst("preferred_username")?.Value ?? "",
            Email: User.FindFirst("email")?.Value ?? "",
            Name: User.FindFirst("name")?.Value ?? "",
            TenantId: User.FindFirst(CustomClaimTypes.TenantId)?.Value ?? "",
            Roles: User.FindAll("groups").Select(c => c.Value).ToList());

        return Ok(user);
    }
}
