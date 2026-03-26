using Asp.Versioning;
using ErrorOr;
using Microsoft.AspNetCore.Mvc;
using ProductCatalog.Application.Features.Category.Commands;
using ProductCatalog.Application.Features.Category.DTOs;
using ProductCatalog.Application.Features.Category.Queries;
using SharedKernel.Api.Authorization;
using SharedKernel.Api.Controllers;
using SharedKernel.Api.ErrorOrMapping;
using SharedKernel.Application.DTOs;
using SharedKernel.Application.Security;
using SharedKernel.Domain.Common;
using Wolverine;

namespace ProductCatalog.Api.Controllers.V1;

[ApiVersion(1.0)]
/// <summary>
/// Presentation-layer controller that exposes CRUD endpoints for product categories,
/// including a stored-procedure-backed statistics query.
/// </summary>
public sealed class CategoriesController(IMessageBus bus) : ApiControllerBase
{
    [HttpGet]
    [RequirePermission(Permission.Categories.Read)]
    public async Task<ActionResult<PagedResponse<CategoryResponse>>> GetAll(
        [FromQuery] CategoryFilter filter,
        CancellationToken ct
    )
    {
        ErrorOr<PagedResponse<CategoryResponse>> result = await bus.InvokeAsync<
            ErrorOr<PagedResponse<CategoryResponse>>
        >(new GetCategoriesQuery(filter), ct);
        return result.ToActionResult(this);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(Permission.Categories.Read)]
    public async Task<ActionResult<CategoryResponse>> GetById(Guid id, CancellationToken ct)
    {
        ErrorOr<CategoryResponse> result = await bus.InvokeAsync<ErrorOr<CategoryResponse>>(
            new GetCategoryByIdQuery(id),
            ct
        );
        return result.ToActionResult(this);
    }

    [HttpPost]
    [RequirePermission(Permission.Categories.Create)]
    public async Task<ActionResult<BatchResponse>> Create(
        CreateCategoriesRequest request,
        CancellationToken ct
    )
    {
        ErrorOr<BatchResponse> result = await bus.InvokeAsync<ErrorOr<BatchResponse>>(
            new CreateCategoriesCommand(request),
            ct
        );
        return result.ToBatchResult(this);
    }

    [HttpPut]
    [RequirePermission(Permission.Categories.Update)]
    public async Task<ActionResult<BatchResponse>> Update(
        UpdateCategoriesRequest request,
        CancellationToken ct
    )
    {
        ErrorOr<BatchResponse> result = await bus.InvokeAsync<ErrorOr<BatchResponse>>(
            new UpdateCategoriesCommand(request),
            ct
        );
        return result.ToBatchResult(this);
    }

    [HttpDelete]
    [RequirePermission(Permission.Categories.Delete)]
    public async Task<ActionResult<BatchResponse>> Delete(
        BatchDeleteRequest request,
        CancellationToken ct
    )
    {
        ErrorOr<BatchResponse> result = await bus.InvokeAsync<ErrorOr<BatchResponse>>(
            new DeleteCategoriesCommand(request),
            ct
        );
        return result.ToBatchResult(this);
    }

    [HttpGet("{id:guid}/stats")]
    [RequirePermission(Permission.Categories.Read)]
    public async Task<ActionResult<ProductCategoryStatsResponse>> GetStats(
        Guid id,
        CancellationToken ct
    )
    {
        ErrorOr<ProductCategoryStatsResponse> result = await bus.InvokeAsync<
            ErrorOr<ProductCategoryStatsResponse>
        >(new GetCategoryStatsQuery(id), ct);
        return result.ToActionResult(this);
    }
}
