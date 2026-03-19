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
public sealed class ProductReviewsController : ApiControllerBase
{
    private readonly ISender _sender;

    public ProductReviewsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [RequirePermission(Permission.ProductReviews.Read)]
    [OutputCache(PolicyName = CachePolicyNames.Reviews)]
    public async Task<ActionResult<PagedResponse<ProductReviewResponse>>> GetAll(
        [FromQuery] ProductReviewFilter filter,
        CancellationToken ct
    )
    {
        var reviews = await _sender.Send(new GetProductReviewsQuery(filter), ct);
        return Ok(reviews);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(Permission.ProductReviews.Read)]
    [OutputCache(PolicyName = CachePolicyNames.Reviews)]
    public async Task<ActionResult<ProductReviewResponse>> GetById(Guid id, CancellationToken ct)
    {
        var review = await _sender.Send(new GetProductReviewByIdQuery(id), ct);
        return OkOrNotFound(review);
    }

    [HttpGet("by-product/{productId:guid}")]
    [RequirePermission(Permission.ProductReviews.Read)]
    [OutputCache(PolicyName = CachePolicyNames.Reviews)]
    public async Task<ActionResult<IEnumerable<ProductReviewResponse>>> GetByProductId(
        Guid productId,
        CancellationToken ct
    )
    {
        var reviews = await _sender.Send(new GetProductReviewsByProductIdQuery(productId), ct);
        return Ok(reviews);
    }

    [HttpPost]
    [RequirePermission(Permission.ProductReviews.Create)]
    public async Task<ActionResult<ProductReviewResponse>> Create(
        CreateProductReviewRequest request,
        CancellationToken ct
    )
    {
        var review = await _sender.Send(new CreateProductReviewCommand(request), ct);
        return CreatedAtGetById(review, review.Id);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(Permission.ProductReviews.Delete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _sender.Send(new DeleteProductReviewCommand(id), ct);
        return NoContent();
    }
}
