using APITemplate.Application.Features.PasswordReset;
using APITemplate.Application.Features.PasswordReset.DTOs;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APITemplate.Api.Controllers.V1;

[ApiVersion(1.0)]
[ApiController]
[Route("api/v{version:apiVersion}/password-reset")]
public sealed class PasswordResetController : ControllerBase
{
    private readonly ISender _sender;

    public PasswordResetController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("request")]
    [AllowAnonymous]
    public async Task<IActionResult> RequestReset(
        RequestPasswordResetRequest request,
        CancellationToken ct
    )
    {
        await _sender.Send(new RequestPasswordResetCommand(request), ct);
        return Ok();
    }

    [HttpPost("confirm")]
    [AllowAnonymous]
    public async Task<IActionResult> Confirm(
        ConfirmPasswordResetRequest request,
        CancellationToken ct
    )
    {
        await _sender.Send(new ConfirmPasswordResetCommand(request), ct);
        return Ok();
    }
}
