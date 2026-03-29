using APITemplate.Api.Authorization;
using APITemplate.Api.Controllers;
using APITemplate.Api.ErrorOrMapping;
using APITemplate.Application.Common.Security;
using APITemplate.Application.Features.TenantInvitation;
using APITemplate.Application.Features.TenantInvitation.DTOs;
using Asp.Versioning;
using ErrorOr;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using SharedKernel.Application.Common.Events;
using Wolverine;

namespace APITemplate.Api.Controllers.V1;

[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/tenant-invitations")]
/// <summary>
/// Presentation-layer controller that manages the lifecycle of tenant invitations,
/// including creation, acceptance via a token link, revocation, and resending.
/// </summary>
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
