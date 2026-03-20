using APITemplate.Api.Authorization;
using APITemplate.Api.Controllers;
using APITemplate.Api.Filters.Idempotency;
using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Common.Security;
using APITemplate.Application.Features.Examples;
using APITemplate.Application.Features.Examples.DTOs;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace APITemplate.Api.Controllers.V1;

[ApiVersion(1.0)]
/// <summary>
/// Presentation-layer controller that demonstrates idempotent POST semantics using the
/// <see cref="Idempotent"/> action filter to detect and short-circuit duplicate requests.
/// </summary>
public sealed class IdempotentController : ApiControllerBase
{
    /// <summary>
    /// Creates a resource idempotently; repeated requests with the same idempotency key
    /// return the original response without re-executing the command.
    /// </summary>
    [HttpPost]
    [Idempotent]
    [RequirePermission(Permission.Examples.Create)]
    public async Task<ActionResult<IdempotentCreateResponse>> Create(
        IdempotentCreateRequest request,
        [FromServices] ICommandHandler<IdempotentCreateCommand, IdempotentCreateResponse> handler,
        CancellationToken ct
    )
    {
        var result = await handler.HandleAsync(new IdempotentCreateCommand(request), ct);
        return Created(string.Empty, result);
    }
}
