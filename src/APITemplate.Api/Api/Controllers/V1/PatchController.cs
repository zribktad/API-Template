using APITemplate.Api.Authorization;
using APITemplate.Application.Features.Examples.DTOs;
using APITemplate.Application.Features.Examples.Handlers;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SystemTextJsonPatch;

namespace APITemplate.Api.Controllers.V1;

[ApiVersion(1.0)]
[ApiController]
[Route("api/v{version:apiVersion}/patch")]
public sealed class PatchController : ControllerBase
{
    private readonly ISender _sender;

    public PatchController(ISender sender) => _sender = sender;

    [HttpPatch("products/{id:guid}")]
    [RequirePermission(Permission.Examples.Update)]
    public async Task<ActionResult<ProductResponse>> PatchProduct(
        Guid id,
        [FromBody] JsonPatchDocument<PatchableProductDto> patchDocument,
        CancellationToken ct
    )
    {
        var result = await _sender.Send(
            new PatchProductCommand(id, dto => patchDocument.ApplyTo(dto)),
            ct
        );
        return Ok(result);
    }
}
