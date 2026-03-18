using APITemplate.Api.Authorization;
using APITemplate.Application.Common.Security;
using APITemplate.Application.Features.Examples.DTOs;
using APITemplate.Application.Features.Examples.Handlers;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace APITemplate.Api.Controllers.V1;

[ApiVersion(1.0)]
[ApiController]
[Route("api/v{version:apiVersion}/batch")]
public sealed class BatchController : ControllerBase
{
    private readonly ISender _sender;

    public BatchController(ISender sender) => _sender = sender;

    [HttpPost("products")]
    [RequirePermission(Permission.Examples.Create)]
    public async Task<ActionResult<BatchCreateProductsResponse>> CreateProducts(
        BatchCreateProductsRequest request,
        CancellationToken ct
    )
    {
        var result = await _sender.Send(new BatchCreateProductsCommand(request), ct);
        return Ok(result);
    }
}
