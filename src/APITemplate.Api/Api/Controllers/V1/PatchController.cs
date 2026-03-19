using APITemplate.Api.Authorization;
using APITemplate.Api.Controllers;
using APITemplate.Application.Features.Examples.DTOs;
using APITemplate.Application.Features.Examples.Handlers;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SystemTextJsonPatch;

namespace APITemplate.Api.Controllers.V1;

[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/patch")]
/// <summary>
/// Presentation-layer controller that demonstrates JSON Patch (RFC 6902) support
/// for partial product updates using <c>SystemTextJsonPatch</c>.
/// </summary>
public sealed class PatchController : ApiControllerBase
{
    private readonly ISender _sender;

    public PatchController(ISender sender) => _sender = sender;

    /// <summary>
    /// Applies a JSON Patch document to the specified product by passing an apply-delegate
    /// to the application layer, which mutates the DTO before persisting.
    /// </summary>
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
