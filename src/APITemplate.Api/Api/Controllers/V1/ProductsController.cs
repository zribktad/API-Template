using APITemplate.Api.Authorization;
using APITemplate.Api.Controllers;
using APITemplate.Application.Common.Security;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using SharedKernel.Application.Common.Events;
using Wolverine;

namespace APITemplate.Api.Controllers.V1;

[ApiVersion(1.0)]
/// <summary>
/// Presentation-layer controller that exposes full CRUD endpoints for the product catalog,
/// with permission-based authorization and tenant-aware output caching.
/// </summary>
public sealed class ProductsController(IMessageBus bus) : ApiControllerBase
{
    [HttpGet]
    [RequirePermission(Permission.Products.Read)]
    [OutputCache(PolicyName = CacheTags.Products)]
    public Task<ActionResult<ProductsResponse>> GetAll(
        [FromQuery] ProductFilter filter,
        CancellationToken ct
    ) => InvokeToActionResultAsync<ProductsResponse>(bus, new GetProductsQuery(filter), ct);

    [HttpGet("{id:guid}")]
    [RequirePermission(Permission.Products.Read)]
    [OutputCache(PolicyName = CacheTags.Products)]
    public Task<ActionResult<ProductResponse>> GetById(Guid id, CancellationToken ct) =>
        InvokeToActionResultAsync<ProductResponse>(bus, new GetProductByIdQuery(id), ct);

    [HttpPost]
    [RequirePermission(Permission.Products.Create)]
    public Task<ActionResult<BatchResponse>> Create(
        CreateProductsRequest request,
        CancellationToken ct
    ) => InvokeToBatchResultAsync(bus, new CreateProductsCommand(request), ct);

    [HttpPut]
    [RequirePermission(Permission.Products.Update)]
    public Task<ActionResult<BatchResponse>> Update(
        UpdateProductsRequest request,
        CancellationToken ct
    ) => InvokeToBatchResultAsync(bus, new UpdateProductsCommand(request), ct);

    [HttpDelete]
    [RequirePermission(Permission.Products.Delete)]
    public Task<ActionResult<BatchResponse>> Delete(
        BatchDeleteRequest request,
        CancellationToken ct
    ) => InvokeToBatchResultAsync(bus, new DeleteProductsCommand(request), ct);
}
