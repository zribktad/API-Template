using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Reviews.Application.Common.Responses;
using Reviews.Application.Features.CreateReview;
using Reviews.Application.Features.DeleteReview;
using Reviews.Application.Features.GetReviewById;
using Reviews.Application.Features.GetReviews;
using Reviews.Application.Features.GetReviewsByProductId;
using SharedKernel.Api.Authorization;
using SharedKernel.Api.Controllers;
using SharedKernel.Api.Extensions;
using SharedKernel.Application.Common.Events;
using SharedKernel.Application.Security;
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
    [HttpGet]
    [RequirePermission(Permission.ProductReviews.Read)]
    [OutputCache(PolicyName = CacheTags.Reviews)]
    public Task<ActionResult<PagedResponse<ProductReviewResponse>>> GetAll(
        [FromQuery] ProductReviewFilter filter,
        CancellationToken ct
    ) =>
        InvokeToActionResultAsync<PagedResponse<ProductReviewResponse>>(
            bus,
            new GetProductReviewsQuery(filter),
            ct
        );

    [HttpGet("{id:guid}")]
    [RequirePermission(Permission.ProductReviews.Read)]
    [OutputCache(PolicyName = CacheTags.Reviews)]
    public Task<ActionResult<ProductReviewResponse>> GetById(Guid id, CancellationToken ct) =>
        InvokeToActionResultAsync<ProductReviewResponse>(
            bus,
            new GetProductReviewByIdQuery(id),
            ct
        );

    [HttpGet("by-product/{productId:guid}")]
    [RequirePermission(Permission.ProductReviews.Read)]
    [OutputCache(PolicyName = CacheTags.Reviews)]
    public Task<ActionResult<IReadOnlyList<ProductReviewResponse>>> GetByProductId(
        Guid productId,
        CancellationToken ct
    ) =>
        InvokeToActionResultAsync<IReadOnlyList<ProductReviewResponse>>(
            bus,
            new GetProductReviewsByProductIdQuery(productId),
            ct
        );

    [HttpPost]
    [RequirePermission(Permission.ProductReviews.Create)]
    public Task<ActionResult<ProductReviewResponse>> Create(
        CreateProductReviewRequest request,
        CancellationToken ct
    ) =>
        InvokeToCreatedResultAsync<ProductReviewResponse>(
            bus,
            new CreateProductReviewCommand(request),
            v => new { id = v.Id, version = this.GetApiVersion() },
            ct
        );

    [HttpDelete("{id:guid}")]
    [RequirePermission(Permission.ProductReviews.Delete)]
    public Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        InvokeToNoContentResultAsync(bus, new DeleteProductReviewCommand(id), ct);
}
