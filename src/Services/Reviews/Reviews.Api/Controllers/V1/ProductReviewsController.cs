using Asp.Versioning;
using ErrorOr;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Reviews.Application.Common.Events;
using Reviews.Application.Common.Security;
using Reviews.Application.Features.ProductReview.Commands;
using Reviews.Application.Features.ProductReview.DTOs;
using Reviews.Application.Features.ProductReview.Queries;
using SharedKernel.Api.Authorization;
using SharedKernel.Api.Controllers;
using SharedKernel.Api.ErrorOrMapping;
using SharedKernel.Api.Extensions;
using SharedKernel.Domain.Common;
using Wolverine;

namespace Reviews.Api.Controllers.V1;

[ApiVersion(1.0)]
/// <summary>
/// Presentation-layer controller that exposes CRUD endpoints for product reviews,
/// with output-cache support and a dedicated by-product lookup endpoint.
/// </summary>
public sealed class ProductReviewsController(IMessageBus bus) : ApiControllerBase
{
    /// <summary>Returns a paginated, filterable list of product reviews.</summary>
    [HttpGet]
    [RequirePermission(Permission.ProductReviews.Read)]
    [OutputCache(PolicyName = CacheTags.Reviews)]
    public async Task<ActionResult<PagedResponse<ProductReviewResponse>>> GetAll(
        [FromQuery] ProductReviewFilter filter,
        CancellationToken ct
    )
    {
        ErrorOr<PagedResponse<ProductReviewResponse>> result = await bus.InvokeAsync<
            ErrorOr<PagedResponse<ProductReviewResponse>>
        >(new GetProductReviewsQuery(filter), ct);
        return result.ToActionResult(this);
    }

    /// <summary>Returns a single product review by its identifier, or 404 if not found.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(Permission.ProductReviews.Read)]
    [OutputCache(PolicyName = CacheTags.Reviews)]
    public async Task<ActionResult<ProductReviewResponse>> GetById(Guid id, CancellationToken ct)
    {
        ErrorOr<ProductReviewResponse> result = await bus.InvokeAsync<
            ErrorOr<ProductReviewResponse>
        >(new GetProductReviewByIdQuery(id), ct);
        return result.ToActionResult(this);
    }

    /// <summary>Returns all reviews for the specified product.</summary>
    [HttpGet("by-product/{productId:guid}")]
    [RequirePermission(Permission.ProductReviews.Read)]
    [OutputCache(PolicyName = CacheTags.Reviews)]
    public async Task<ActionResult<IReadOnlyList<ProductReviewResponse>>> GetByProductId(
        Guid productId,
        CancellationToken ct
    )
    {
        ErrorOr<IReadOnlyList<ProductReviewResponse>> result = await bus.InvokeAsync<
            ErrorOr<IReadOnlyList<ProductReviewResponse>>
        >(new GetProductReviewsByProductIdQuery(productId), ct);
        return result.ToActionResult(this);
    }

    /// <summary>Creates a new product review and returns it with a 201 Location header.</summary>
    [HttpPost]
    [RequirePermission(Permission.ProductReviews.Create)]
    public async Task<ActionResult<ProductReviewResponse>> Create(
        CreateProductReviewRequest request,
        CancellationToken ct
    )
    {
        ErrorOr<ProductReviewResponse> result = await bus.InvokeAsync<
            ErrorOr<ProductReviewResponse>
        >(new CreateProductReviewCommand(request), ct);
        return result.ToCreatedResult(this, v => new { id = v.Id, version = this.GetApiVersion() });
    }

    /// <summary>Deletes a product review by its identifier.</summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission(Permission.ProductReviews.Delete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        ErrorOr<Success> result = await bus.InvokeAsync<ErrorOr<Success>>(
            new DeleteProductReviewCommand(id),
            ct
        );
        return result.ToNoContentResult(this);
    }
}
