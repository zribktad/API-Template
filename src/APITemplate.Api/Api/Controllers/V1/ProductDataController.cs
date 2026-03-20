using APITemplate.Api.Authorization;
using APITemplate.Api.Controllers;
using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Common.Security;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace APITemplate.Api.Controllers.V1;

[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/product-data")]
/// <summary>
/// Presentation-layer controller that manages product supplementary data (images and videos)
/// stored in MongoDB, with output-cache integration for read endpoints.
/// </summary>
public sealed class ProductDataController : ApiControllerBase
{
    /// <summary>Returns all product data documents, optionally filtered by type.</summary>
    [HttpGet]
    [RequirePermission(Permission.ProductData.Read)]
    [OutputCache(PolicyName = CacheTags.ProductData)]
    public async Task<ActionResult<List<ProductDataResponse>>> GetAll(
        [FromQuery] string? type,
        [FromServices] IQueryHandler<GetProductDataQuery, List<ProductDataResponse>> handler,
        CancellationToken ct
    )
    {
        var items = await handler.HandleAsync(new GetProductDataQuery(type), ct);
        return Ok(items);
    }

    /// <summary>Returns a single product data document by its identifier, or 404 if not found.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(Permission.ProductData.Read)]
    [OutputCache(PolicyName = CacheTags.ProductData)]
    public async Task<ActionResult<ProductDataResponse>> GetById(
        Guid id,
        [FromServices] IQueryHandler<GetProductDataByIdQuery, ProductDataResponse?> handler,
        CancellationToken ct
    )
    {
        var item = await handler.HandleAsync(new GetProductDataByIdQuery(id), ct);
        return OkOrNotFound(item);
    }

    /// <summary>Creates a new image product-data document and returns it with a 201 Location header.</summary>
    [HttpPost("image")]
    [RequirePermission(Permission.ProductData.Create)]
    public async Task<ActionResult<ProductDataResponse>> CreateImage(
        CreateImageProductDataRequest request,
        [FromServices] ICommandHandler<CreateImageProductDataCommand, ProductDataResponse> handler,
        CancellationToken ct
    )
    {
        var created = await handler.HandleAsync(new CreateImageProductDataCommand(request), ct);
        return CreatedAtGetById(created, created.Id);
    }

    /// <summary>Creates a new video product-data document and returns it with a 201 Location header.</summary>
    [HttpPost("video")]
    [RequirePermission(Permission.ProductData.Create)]
    public async Task<ActionResult<ProductDataResponse>> CreateVideo(
        CreateVideoProductDataRequest request,
        [FromServices] ICommandHandler<CreateVideoProductDataCommand, ProductDataResponse> handler,
        CancellationToken ct
    )
    {
        var created = await handler.HandleAsync(new CreateVideoProductDataCommand(request), ct);
        return CreatedAtGetById(created, created.Id);
    }

    /// <summary>Deletes a product data document by its identifier.</summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission(Permission.ProductData.Delete)]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] ICommandHandler<DeleteProductDataCommand> handler,
        CancellationToken ct
    )
    {
        await handler.HandleAsync(new DeleteProductDataCommand(id), ct);
        return NoContent();
    }
}
