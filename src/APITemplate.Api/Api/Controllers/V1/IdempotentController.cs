using APITemplate.Api.Authorization;
using APITemplate.Api.Filters;
using APITemplate.Application.Common.Security;
using APITemplate.Application.Features.Examples.DTOs;
using APITemplate.Application.Features.Examples.Handlers;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace APITemplate.Api.Controllers.V1;

[ApiVersion(1.0)]
[ApiController]
[Route("api/v{version:apiVersion}/idempotent")]
/// <summary>
/// Presentation-layer controller that demonstrates idempotent POST semantics using the
/// <see cref="Idempotent"/> action filter to detect and short-circuit duplicate requests.
/// </summary>
public sealed class IdempotentController : ControllerBase
{
    private readonly ISender _sender;

    public IdempotentController(ISender sender) => _sender = sender;

    /// <summary>
    /// Creates a resource idempotently; repeated requests with the same idempotency key
    /// return the original response without re-executing the command.
    /// </summary>
    [HttpPost]
    [Idempotent]
    [RequirePermission(Permission.Examples.Create)]
    public async Task<ActionResult<IdempotentCreateResponse>> Create(
        IdempotentCreateRequest request,
        CancellationToken ct
    )
    {
        var result = await _sender.Send(new IdempotentCreateCommand(request), ct);
        return Created(string.Empty, result);
    }
}
