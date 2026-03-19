using APITemplate.Api.Authorization;
using APITemplate.Api.Cache;
using APITemplate.Api.Controllers;
using APITemplate.Application.Common.Security;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace APITemplate.Api.Controllers.V1;

[ApiVersion(1.0)]
public sealed class ProductsController : ApiControllerBase
{
    private readonly ISender _sender;

    public ProductsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [RequirePermission(Permission.Products.Read)]
    [OutputCache(PolicyName = CachePolicyNames.Products)]
    public async Task<ActionResult<ProductsResponse>> GetAll(
        [FromQuery] ProductFilter filter,
        CancellationToken ct
    )
    {
        var products = await _sender.Send(new GetProductsQuery(filter), ct);
        return Ok(products);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(Permission.Products.Read)]
    [OutputCache(PolicyName = CachePolicyNames.Products)]
    public async Task<ActionResult<ProductResponse>> GetById(Guid id, CancellationToken ct)
    {
        var product = await _sender.Send(new GetProductByIdQuery(id), ct);
        return OkOrNotFound(product);
    }

    [HttpPost]
    [RequirePermission(Permission.Products.Create)]
    public async Task<ActionResult<ProductResponse>> Create(
        CreateProductRequest request,
        CancellationToken ct
    )
    {
        var product = await _sender.Send(new CreateProductCommand(request), ct);
        return CreatedAtGetById(product, product.Id);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(Permission.Products.Update)]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateProductRequest request,
        CancellationToken ct
    )
    {
        await _sender.Send(new UpdateProductCommand(id, request), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(Permission.Products.Delete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _sender.Send(new DeleteProductCommand(id), ct);
        return NoContent();
    }
}
