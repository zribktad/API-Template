using APITemplate.Api.Authorization;
using APITemplate.Api.Controllers;
using APITemplate.Application.Common.Security;
using APITemplate.Application.Features.Examples.DTOs;
using APITemplate.Application.Features.Examples.Handlers;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace APITemplate.Api.Controllers.V1;

[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/batch")]
/// <summary>
/// Presentation-layer controller that demonstrates bulk-creation patterns by accepting
/// a collection of products in a single HTTP request and dispatching them via MediatR.
/// </summary>
public sealed class BatchController : ApiControllerBase
{
    private readonly ISender _sender;

    public BatchController(ISender sender) => _sender = sender;

    /// <summary>
    /// Creates multiple products in a single request and returns the batch result.
    /// </summary>
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
