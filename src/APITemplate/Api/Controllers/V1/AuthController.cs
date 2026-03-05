using APITemplate.Application.Features.Auth.Interfaces;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APITemplate.Api.Controllers.V1;

[ApiVersion(1.0)]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthenticationProxy _authProxy;

    public AuthController(IAuthenticationProxy authProxy)
    {
        _authProxy = authProxy;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var token = await _authProxy.AuthenticateAsync(request.Username, request.Password, ct);
        if (token is null)
            return Unauthorized(new LoginErrorResponse("Invalid username or password."));

        return Ok(token);
    }
}
