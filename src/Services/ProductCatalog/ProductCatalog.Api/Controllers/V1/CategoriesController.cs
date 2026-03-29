using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using ProductCatalog.Application.Features.Category.Commands;
using ProductCatalog.Application.Features.Category.DTOs;
using ProductCatalog.Application.Features.Category.Queries;
using SharedKernel.Api.Authorization;
using SharedKernel.Api.Controllers;
using SharedKernel.Application.Common.Events;
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
    [OutputCache(PolicyName = CacheTags.Categories)]
    public Task<ActionResult<PagedResponse<CategoryResponse>>> GetAll(
        [FromQuery] CategoryFilter filter,
        CancellationToken ct
    ) =>
        InvokeToActionResultAsync<PagedResponse<CategoryResponse>>(
            bus,
            new GetCategoriesQuery(filter),
            ct
        );

    [HttpGet("{id:guid}")]
    [RequirePermission(Permission.Categories.Read)]
    [OutputCache(PolicyName = CacheTags.Categories)]
    public Task<ActionResult<CategoryResponse>> GetById(Guid id, CancellationToken ct) =>
        InvokeToActionResultAsync<CategoryResponse>(bus, new GetCategoryByIdQuery(id), ct);

    [HttpPost]
    [RequirePermission(Permission.Categories.Create)]
    public Task<ActionResult<BatchResponse>> Create(
        CreateCategoriesRequest request,
        CancellationToken ct
    ) => InvokeToBatchResultAsync(bus, new CreateCategoriesCommand(request), ct);

    [HttpPut]
    [RequirePermission(Permission.Categories.Update)]
    public Task<ActionResult<BatchResponse>> Update(
        UpdateCategoriesRequest request,
        CancellationToken ct
    ) => InvokeToBatchResultAsync(bus, new UpdateCategoriesCommand(request), ct);

    [HttpDelete]
    [RequirePermission(Permission.Categories.Delete)]
    public Task<ActionResult<BatchResponse>> Delete(
        BatchDeleteRequest request,
        CancellationToken ct
    ) => InvokeToBatchResultAsync(bus, new DeleteCategoriesCommand(request), ct);

    [HttpGet("{id:guid}/stats")]
    [RequirePermission(Permission.Categories.Read)]
    [OutputCache(PolicyName = CacheTags.Categories)]
    public Task<ActionResult<ProductCategoryStatsResponse>> GetStats(
        Guid id,
        CancellationToken ct
    ) =>
        InvokeToActionResultAsync<ProductCategoryStatsResponse>(
            bus,
            new GetCategoryStatsQuery(id),
            ct
        );
}
