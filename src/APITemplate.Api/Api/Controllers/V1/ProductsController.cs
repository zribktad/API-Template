using APITemplate.Api.Authorization;
using APITemplate.Api.Cache;
using APITemplate.Api.Controllers;
using APITemplate.Application.Common.CQRS;
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
    [OutputCache(PolicyName = CachePolicyNames.Products)]
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
    [OutputCache(PolicyName = CachePolicyNames.Products)]
    public async Task<ActionResult<ProductResponse>> GetById(
        Guid id,
        [FromServices] IQueryHandler<GetProductByIdQuery, ProductResponse?> handler,
        CancellationToken ct
    )
    {
        var product = await handler.HandleAsync(new GetProductByIdQuery(id), ct);
        return OkOrNotFound(product);
    }

    /// <summary>Creates a new product and returns it with a 201 Location header.</summary>
    [HttpPost]
    [RequirePermission(Permission.Products.Create)]
    public async Task<ActionResult<ProductResponse>> Create(
        CreateProductRequest request,
        [FromServices] ICommandHandler<CreateProductCommand, ProductResponse> handler,
        CancellationToken ct
    )
    {
        var product = await handler.HandleAsync(new CreateProductCommand(request), ct);
        return CreatedAtGetById(product, product.Id);
    }

    /// <summary>Replaces all mutable fields of an existing product.</summary>
    [HttpPut("{id:guid}")]
    [RequirePermission(Permission.Products.Update)]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateProductRequest request,
        [FromServices] ICommandHandler<UpdateProductCommand> handler,
        CancellationToken ct
    )
    {
        await handler.HandleAsync(new UpdateProductCommand(id, request), ct);
        return NoContent();
    }

    /// <summary>Soft-deletes a product by its identifier.</summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission(Permission.Products.Delete)]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] ICommandHandler<DeleteProductCommand> handler,
        CancellationToken ct
    )
    {
        await handler.HandleAsync(new DeleteProductCommand(id), ct);
        return NoContent();
    }
}
