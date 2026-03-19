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
/// <summary>
/// Presentation-layer controller that exposes CRUD endpoints for product categories,
/// including a stored-procedure-backed statistics query.
/// </summary>
public sealed class CategoriesController : ApiControllerBase
{
    private readonly ISender _sender;

    public CategoriesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Returns a paginated, filterable list of categories from the output cache.
    /// </summary>
    [HttpGet]
    [RequirePermission(Permission.Categories.Read)]
    [OutputCache(PolicyName = CachePolicyNames.Categories)]
    public async Task<ActionResult<PagedResponse<CategoryResponse>>> GetAll(
        [FromQuery] CategoryFilter filter,
        CancellationToken ct
    )
    {
        var categories = await _sender.Send(new GetCategoriesQuery(filter), ct);
        return Ok(categories);
    }

    /// <summary>Returns a single category by its identifier, or 404 if not found.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(Permission.Categories.Read)]
    [OutputCache(PolicyName = CachePolicyNames.Categories)]
    public async Task<ActionResult<CategoryResponse>> GetById(Guid id, CancellationToken ct)
    {
        var category = await _sender.Send(new GetCategoryByIdQuery(id), ct);
        return OkOrNotFound(category);
    }

    /// <summary>Creates a new category and returns it with a 201 Location header.</summary>
    [HttpPost]
    [RequirePermission(Permission.Categories.Create)]
    public async Task<ActionResult<CategoryResponse>> Create(
        CreateCategoryRequest request,
        CancellationToken ct
    )
    {
        var category = await _sender.Send(new CreateCategoryCommand(request), ct);
        return CreatedAtGetById(category, category.Id);
    }

    /// <summary>Replaces all mutable fields of an existing category.</summary>
    [HttpPut("{id:guid}")]
    [RequirePermission(Permission.Categories.Update)]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateCategoryRequest request,
        CancellationToken ct
    )
    {
        await _sender.Send(new UpdateCategoryCommand(id, request), ct);
        return NoContent();
    }

    /// <summary>Soft-deletes a category by its identifier.</summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission(Permission.Categories.Delete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _sender.Send(new DeleteCategoryCommand(id), ct);
        return NoContent();
    }

    /// <summary>
    /// Returns aggregated statistics for a category by calling the
    /// <c>get_product_category_stats(p_category_id)</c> stored procedure via EF Core FromSql.
    /// </summary>
    [HttpGet("{id:guid}/stats")]
    [RequirePermission(Permission.Categories.Read)]
    [OutputCache(PolicyName = CachePolicyNames.Categories)]
    public async Task<ActionResult<ProductCategoryStatsResponse>> GetStats(
        Guid id,
        CancellationToken ct
    )
    {
        var stats = await _sender.Send(new GetCategoryStatsQuery(id), ct);
        return OkOrNotFound(stats);
    }
}
