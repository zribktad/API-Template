using Asp.Versioning;
using Identity.Application.Features.Tenant.Commands;
using Identity.Application.Features.Tenant.DTOs;
using Identity.Application.Features.Tenant.Queries;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using SharedKernel.Api.Authorization;
using SharedKernel.Api.Controllers;
using SharedKernel.Api.Extensions;
using SharedKernel.Application.Common.Events;
using SharedKernel.Application.Security;
using SharedKernel.Domain.Common;
using Wolverine;

namespace Identity.Api.Controllers.V1;

[ApiVersion(1.0)]
public sealed class TenantsController(IMessageBus bus) : ApiControllerBase
{
    [HttpGet]
    [RequirePermission(Permission.Tenants.Read)]
    [OutputCache(PolicyName = CacheTags.Tenants)]
    public Task<ActionResult<PagedResponse<TenantResponse>>> GetAll(
        [FromQuery] TenantFilter filter,
        CancellationToken ct
    ) =>
        InvokeToActionResultAsync<PagedResponse<TenantResponse>>(
            bus,
            new GetTenantsQuery(filter),
            ct
        );

    [HttpGet("{id:guid}")]
    [RequirePermission(Permission.Tenants.Read)]
    [OutputCache(PolicyName = CacheTags.Tenants)]
    public Task<ActionResult<TenantResponse>> GetById(Guid id, CancellationToken ct) =>
        InvokeToActionResultAsync<TenantResponse>(bus, new GetTenantByIdQuery(id), ct);

    [HttpPost]
    [RequirePermission(Permission.Tenants.Create)]
    public Task<ActionResult<TenantResponse>> Create(
        CreateTenantRequest request,
        CancellationToken ct
    ) =>
        InvokeToCreatedResultAsync<TenantResponse>(
            bus,
            new CreateTenantCommand(request),
            v => new { id = v.Id, version = this.GetApiVersion() },
            ct
        );

    [HttpDelete("{id:guid}")]
    [RequirePermission(Permission.Tenants.Delete)]
    public Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        InvokeToNoContentResultAsync(bus, new DeleteTenantCommand(id), ct);
}
