using APITemplate.Api.Authorization;
using APITemplate.Api.Cache;
using APITemplate.Application.Common.Security;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace APITemplate.Api.Controllers.V1;

[ApiVersion(1.0)]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
/// <summary>
/// Presentation-layer controller that exposes CRUD endpoints for product reviews,
/// with output-cache support and a dedicated by-product lookup endpoint.
/// </summary>
public sealed class ProductReviewsController : ControllerBase
{
    private readonly ISender _sender;

    public ProductReviewsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Returns a paginated, filterable list of product reviews.</summary>
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

    /// <summary>Returns a single product review by its identifier, or 404 if not found.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(Permission.ProductReviews.Read)]
    [OutputCache(PolicyName = CachePolicyNames.Reviews)]
    public async Task<ActionResult<ProductReviewResponse>> GetById(Guid id, CancellationToken ct)
    {
        var review = await _sender.Send(new GetProductReviewByIdQuery(id), ct);
        return review is null ? NotFound() : Ok(review);
    }

    /// <summary>Returns all reviews for the specified product.</summary>
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

    /// <summary>Creates a new product review and returns it with a 201 Location header.</summary>
    [HttpPost]
    [RequirePermission(Permission.ProductReviews.Create)]
    public async Task<ActionResult<ProductReviewResponse>> Create(
        CreateProductReviewRequest request,
        CancellationToken ct
    )
    {
        var review = await _sender.Send(new CreateProductReviewCommand(request), ct);
        return CreatedAtAction(
            nameof(GetById),
            new { id = review.Id, version = this.GetApiVersion() },
            review
        );
    }

    /// <summary>Deletes a product review by its identifier.</summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission(Permission.ProductReviews.Delete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _sender.Send(new DeleteProductReviewCommand(id), ct);
        return NoContent();
    }
}
