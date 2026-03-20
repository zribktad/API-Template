using APITemplate.Api.Authorization;
using APITemplate.Api.Controllers;
using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Common.Security;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace APITemplate.Api.Controllers.V1;

[ApiVersion(1.0)]
/// <summary>
/// Presentation-layer controller that exposes CRUD endpoints for tenant management,
/// restricted to platform-level permissions with tenant-isolated output caching.
/// </summary>
public sealed class TenantsController : ApiControllerBase
{
    /// <summary>Returns a paginated, filterable list of tenants.</summary>
    [HttpGet]
    [RequirePermission(Permission.Tenants.Read)]
    [OutputCache(PolicyName = CacheTags.Tenants)]
    public async Task<ActionResult<PagedResponse<TenantResponse>>> GetAll(
        [FromQuery] TenantFilter filter,
        [FromServices] IQueryHandler<GetTenantsQuery, PagedResponse<TenantResponse>> handler,
        CancellationToken ct
    )
    {
        var tenants = await handler.HandleAsync(new GetTenantsQuery(filter), ct);
        return Ok(tenants);
    }

    /// <summary>Returns a single tenant by its identifier, or 404 if not found.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(Permission.Tenants.Read)]
    [OutputCache(PolicyName = CacheTags.Tenants)]
    public async Task<ActionResult<TenantResponse>> GetById(
        Guid id,
        [FromServices] IQueryHandler<GetTenantByIdQuery, TenantResponse?> handler,
        CancellationToken ct
    )
    {
        var tenant = await handler.HandleAsync(new GetTenantByIdQuery(id), ct);
        return OkOrNotFound(tenant);
    }

    /// <summary>Creates a new tenant and returns it with a 201 Location header.</summary>
    [HttpPost]
    [RequirePermission(Permission.Tenants.Create)]
    public async Task<ActionResult<TenantResponse>> Create(
        CreateTenantRequest request,
        [FromServices] ICommandHandler<CreateTenantCommand, TenantResponse> handler,
        CancellationToken ct
    )
    {
        var tenant = await handler.HandleAsync(new CreateTenantCommand(request), ct);
        return CreatedAtGetById(tenant, tenant.Id);
    }

    /// <summary>Soft-deletes a tenant and cascades the deletion to its child entities.</summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission(Permission.Tenants.Delete)]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] ICommandHandler<DeleteTenantCommand> handler,
        CancellationToken ct
    )
    {
        await handler.HandleAsync(new DeleteTenantCommand(id), ct);
        return NoContent();
    }
}
