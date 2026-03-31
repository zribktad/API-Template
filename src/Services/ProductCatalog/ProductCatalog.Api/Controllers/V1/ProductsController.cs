using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using ProductCatalog.Application.Features.Product.Commands;
using ProductCatalog.Application.Features.Product.DTOs;
using ProductCatalog.Application.Features.Product.Queries;
using SharedKernel.Api.Authorization;
using SharedKernel.Api.Controllers;
using SharedKernel.Application.Common.Events;
using SharedKernel.Application.DTOs;
using SharedKernel.Application.Security;
using Wolverine;

namespace ProductCatalog.Api.Controllers.V1;

[ApiVersion(1.0)]
/// <summary>
/// Presentation-layer controller that exposes full CRUD endpoints for the product catalog.
/// </summary>
public sealed class ProductsController(IMessageBus bus) : ApiControllerBase
{
    /// <summary>Returns a filtered, paginated product list including search facets.</summary>
    [HttpGet]
    [RequirePermission(Permission.Products.Read)]
    [OutputCache(PolicyName = CacheTags.Products)]
    public Task<ActionResult<ProductsResponse>> GetAll(
        [FromQuery] ProductFilter filter,
        CancellationToken ct
    ) => InvokeToActionResultAsync<ProductsResponse>(bus, new GetProductsQuery(filter), ct);

    /// <summary>Returns a single product by its identifier, or 404 if not found.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(Permission.Products.Read)]
    [OutputCache(PolicyName = CacheTags.Products)]
    public Task<ActionResult<ProductResponse>> GetById(Guid id, CancellationToken ct) =>
        InvokeToActionResultAsync<ProductResponse>(bus, new GetProductByIdQuery(id), ct);

    /// <summary>Creates multiple products in a single batch operation.</summary>
    [HttpPost]
    [RequirePermission(Permission.Products.Create)]
    public Task<ActionResult<BatchResponse>> Create(
        CreateProductsRequest request,
        CancellationToken ct
    ) => InvokeToBatchResultAsync(bus, new CreateProductsCommand(request), ct);

    /// <summary>Updates multiple products in a single batch operation.</summary>
    [HttpPut]
    [RequirePermission(Permission.Products.Update)]
    public Task<ActionResult<BatchResponse>> Update(
        UpdateProductsRequest request,
        CancellationToken ct
    ) => InvokeToBatchResultAsync(bus, new UpdateProductsCommand(request), ct);

    /// <summary>Soft-deletes multiple products in a single batch operation.</summary>
    [HttpDelete]
    [RequirePermission(Permission.Products.Delete)]
    public Task<ActionResult<BatchResponse>> Delete(
        BatchDeleteRequest request,
        CancellationToken ct
    ) => InvokeToBatchResultAsync(bus, new DeleteProductsCommand(request), ct);
}
