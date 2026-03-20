using APITemplate.Api.Authorization;
using APITemplate.Api.Cache;
using APITemplate.Api.Controllers;
using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Common.DTOs;
using APITemplate.Application.Common.Security;
using APITemplate.Application.Features.TenantInvitation;
using APITemplate.Application.Features.TenantInvitation.DTOs;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace APITemplate.Api.Controllers.V1;

[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/tenant-invitations")]
/// <summary>
/// Presentation-layer controller that manages the lifecycle of tenant invitations,
/// including creation, acceptance via a token link, revocation, and resending.
/// </summary>
public sealed class TenantInvitationsController : ApiControllerBase
{
    /// <summary>Returns a paginated list of tenant invitations, optionally filtered.</summary>
    [HttpGet]
    [RequirePermission(Permission.Invitations.Read)]
    [OutputCache(PolicyName = CachePolicyNames.TenantInvitations)]
    public async Task<ActionResult<PagedResponse<TenantInvitationResponse>>> GetAll(
        [FromQuery] TenantInvitationFilter filter,
        [FromServices]
            IQueryHandler<
            GetTenantInvitationsQuery,
            PagedResponse<TenantInvitationResponse>
        > handler,
        CancellationToken ct
    )
    {
        var result = await handler.HandleAsync(new GetTenantInvitationsQuery(filter), ct);
        return Ok(result);
    }

    /// <summary>Creates a new tenant invitation and sends the invite email.</summary>
    [HttpPost]
    [RequirePermission(Permission.Invitations.Create)]
    public async Task<ActionResult<TenantInvitationResponse>> Create(
        CreateTenantInvitationRequest request,
        [FromServices]
            ICommandHandler<CreateTenantInvitationCommand, TenantInvitationResponse> handler,
        CancellationToken ct
    )
    {
        var invitation = await handler.HandleAsync(new CreateTenantInvitationCommand(request), ct);
        return CreatedAtAction(nameof(GetAll), new { version = this.GetApiVersion() }, invitation);
    }

    /// <summary>Accepts a pending invitation using the one-time token from the invite email; allows anonymous callers.</summary>
    [HttpPost("accept")]
    [AllowAnonymous]
    public async Task<IActionResult> Accept(
        [FromBody] AcceptInvitationRequest request,
        [FromServices] ICommandHandler<AcceptTenantInvitationCommand> handler,
        CancellationToken ct
    )
    {
        await handler.HandleAsync(new AcceptTenantInvitationCommand(request.Token), ct);
        return Ok();
    }

    /// <summary>Marks an outstanding invitation as revoked so the token can no longer be accepted.</summary>
    [HttpPatch("{id:guid}/revoke")]
    [RequirePermission(Permission.Invitations.Revoke)]
    public async Task<IActionResult> Revoke(
        Guid id,
        [FromServices] ICommandHandler<RevokeTenantInvitationCommand> handler,
        CancellationToken ct
    )
    {
        await handler.HandleAsync(new RevokeTenantInvitationCommand(id), ct);
        return NoContent();
    }

    /// <summary>Re-sends the invitation email for a pending invitation that has not yet been accepted or revoked.</summary>
    [HttpPost("{id:guid}/resend")]
    [RequirePermission(Permission.Invitations.Create)]
    public async Task<IActionResult> Resend(
        Guid id,
        [FromServices] ICommandHandler<ResendTenantInvitationCommand> handler,
        CancellationToken ct
    )
    {
        await handler.HandleAsync(new ResendTenantInvitationCommand(id), ct);
        return Ok();
    }
}
