using APITemplate.Api.Authorization;
using APITemplate.Api.Controllers;
using APITemplate.Api.Filters.Idempotency;
using APITemplate.Application.Common.Security;
using APITemplate.Application.Features.Examples.DTOs;
using APITemplate.Application.Features.Examples.Handlers;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace APITemplate.Api.Controllers.V1;

[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/idempotent")]
public sealed class IdempotentController : ApiControllerBase
{
    private readonly ISender _sender;

    public IdempotentController(ISender sender) => _sender = sender;

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
