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
/// <summary>
/// Presentation-layer controller that exposes full CRUD endpoints for the product catalog,
/// with permission-based authorization and tenant-aware output caching.
/// </summary>
public sealed class ProductsController : ApiControllerBase
{
    /// <summary>Returns a filtered, paginated product list including search facets.</summary>
    [HttpGet]
    [RequirePermission(Permission.Products.Read)]
    [OutputCache(PolicyName = CacheTags.Products)]
    public async Task<ActionResult<ProductsResponse>> GetAll(
        [FromQuery] ProductFilter filter,
        [FromServices] IQueryHandler<GetProductsQuery, ProductsResponse> handler,
        CancellationToken ct
    )
    {
        var products = await handler.HandleAsync(new GetProductsQuery(filter), ct);
        return Ok(products);
    }

    /// <summary>Returns a single product by its identifier, or 404 if not found.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(Permission.Products.Read)]
    [OutputCache(PolicyName = CacheTags.Products)]
    public async Task<ActionResult<ProductResponse>> GetById(
        Guid id,
        [FromServices] IQueryHandler<GetProductByIdQuery, ProductResponse?> handler,
        CancellationToken ct
    )
    {
        var product = await handler.HandleAsync(new GetProductByIdQuery(id), ct);
        return OkOrNotFound(product);
    }

    /// <summary>Creates multiple products in a single batch operation.</summary>
    [HttpPost]
    [RequirePermission(Permission.Products.Create)]
    public async Task<ActionResult<BatchResponse>> Create(
        CreateProductsRequest request,
        [FromServices] ICommandHandler<CreateProductsCommand, BatchResponse> handler,
        CancellationToken ct
    )
    {
        var result = await handler.HandleAsync(new CreateProductsCommand(request), ct);
        return OkOrUnprocessable(result);
    }

    /// <summary>Updates multiple products in a single batch operation.</summary>
    [HttpPut]
    [RequirePermission(Permission.Products.Update)]
    public async Task<ActionResult<BatchResponse>> Update(
        UpdateProductsRequest request,
        [FromServices] ICommandHandler<UpdateProductsCommand, BatchResponse> handler,
        CancellationToken ct
    )
    {
        var result = await handler.HandleAsync(new UpdateProductsCommand(request), ct);
        return OkOrUnprocessable(result);
    }

    /// <summary>Soft-deletes multiple products in a single batch operation.</summary>
    [HttpDelete]
    [RequirePermission(Permission.Products.Delete)]
    public async Task<ActionResult<BatchResponse>> Delete(
        BatchDeleteRequest request,
        [FromServices] ICommandHandler<DeleteProductsCommand, BatchResponse> handler,
        CancellationToken ct
    )
    {
        var result = await handler.HandleAsync(new DeleteProductsCommand(request), ct);
        return OkOrUnprocessable(result);
    }
}
