using Asp.Versioning;
using ErrorOr;
using Identity.Application.Features.TenantInvitation.Commands;
using Identity.Application.Features.TenantInvitation.DTOs;
using Identity.Application.Features.TenantInvitation.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using SharedKernel.Api.Authorization;
using SharedKernel.Api.Controllers;
using SharedKernel.Api.ErrorOrMapping;
using SharedKernel.Api.Extensions;
using SharedKernel.Api.Filters.Idempotency;
using SharedKernel.Application.Common.Events;
using SharedKernel.Application.Security;
using SharedKernel.Domain.Common;
using Wolverine;

namespace Identity.Api.Controllers.V1;

[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class TenantInvitationsController(IMessageBus bus) : ApiControllerBase
{
    [HttpGet]
    [RequirePermission(Permission.Invitations.Read)]
    [OutputCache(PolicyName = CacheTags.TenantInvitations)]
    public Task<ActionResult<PagedResponse<TenantInvitationResponse>>> GetAll(
        [FromQuery] TenantInvitationFilter filter,
        CancellationToken ct
    ) =>
        InvokeToActionResultAsync<PagedResponse<TenantInvitationResponse>>(
            bus,
            new GetTenantInvitationsQuery(filter),
            ct
        );

    [HttpPost]
    [Idempotent]
    [RequirePermission(Permission.Invitations.Create)]
    public async Task<ActionResult<TenantInvitationResponse>> Create(
        CreateTenantInvitationRequest request,
        CancellationToken ct
    )
    {
        ErrorOr<TenantInvitationResponse> result = await bus.InvokeAsync<
            ErrorOr<TenantInvitationResponse>
        >(new CreateTenantInvitationCommand(request), ct);
        if (result.IsError)
            return result.ToActionResult(this);

        return CreatedAtAction(
            nameof(GetAll),
            new { version = this.GetApiVersion() },
            result.Value
        );
    }

    [HttpPost("accept")]
    [AllowAnonymous]
    public Task<IActionResult> Accept(
        [FromBody] AcceptInvitationRequest request,
        CancellationToken ct
    ) => InvokeToOkResultAsync(bus, new AcceptTenantInvitationCommand(request.Token), ct);

    [HttpPatch("{id:guid}/revoke")]
    [RequirePermission(Permission.Invitations.Revoke)]
    public Task<IActionResult> Revoke(Guid id, CancellationToken ct) =>
        InvokeToNoContentResultAsync(bus, new RevokeTenantInvitationCommand(id), ct);

    [HttpPost("{id:guid}/resend")]
    [RequirePermission(Permission.Invitations.Create)]
    public Task<IActionResult> Resend(Guid id, CancellationToken ct) =>
        InvokeToOkResultAsync(bus, new ResendTenantInvitationCommand(id), ct);
}
