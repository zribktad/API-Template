using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using APITemplate.Api.Authorization;
using APITemplate.Api.Cache;
using APITemplate.Application.Common.DTOs;
using APITemplate.Application.Common.Security;
using APITemplate.Application.Features.User.DTOs;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace APITemplate.Api.Controllers.V1;

[ApiVersion(1.0)]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
/// <summary>
/// Presentation-layer controller that exposes user management endpoints including
/// CRUD operations, activation/deactivation, role changes, and self-service password reset.
/// </summary>
public sealed class UsersController : ControllerBase
{
    private readonly ISender _sender;

    public UsersController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Returns a paginated, filterable list of users.</summary>
    [HttpGet]
    [RequirePermission(Permission.Users.Read)]
    [OutputCache(PolicyName = CachePolicyNames.Users)]
    public async Task<ActionResult<PagedResponse<UserResponse>>> GetAll(
        [FromQuery] UserFilter filter,
        CancellationToken ct
    )
    {
        var result = await _sender.Send(new GetUsersQuery(filter), ct);
        return Ok(result);
    }

    /// <summary>Returns a single user by their identifier, or 404 if not found.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(Permission.Users.Read)]
    [OutputCache(PolicyName = CachePolicyNames.Users)]
    public async Task<ActionResult<UserResponse>> GetById(Guid id, CancellationToken ct)
    {
        var user = await _sender.Send(new GetUserByIdQuery(id), ct);
        return user is null ? NotFound() : Ok(user);
    }

    /// <summary>
    /// Returns the currently authenticated user's profile by resolving their id from the
    /// JWT/cookie claims (<c>NameIdentifier</c>, <c>sub</c>, or a custom subject claim).
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> GetMe(CancellationToken ct)
    {
        var userId =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(AuthConstants.Claims.Subject);

        if (userId is null || !Guid.TryParse(userId, out var id))
            return Unauthorized();

        var user = await _sender.Send(new GetUserByIdQuery(id), ct);
        return user is null ? NotFound() : Ok(user);
    }

    /// <summary>Creates a new user account and returns it with a 201 Location header.</summary>
    [HttpPost]
    [RequirePermission(Permission.Users.Create)]
    public async Task<ActionResult<UserResponse>> Create(
        CreateUserRequest request,
        CancellationToken ct
    )
    {
        var user = await _sender.Send(new CreateUserCommand(request), ct);
        return CreatedAtAction(
            nameof(GetById),
            new { id = user.Id, version = this.GetApiVersion() },
            user
        );
    }

    /// <summary>Replaces all mutable fields of an existing user.</summary>
    [HttpPut("{id:guid}")]
    [RequirePermission(Permission.Users.Update)]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateUserRequest request,
        CancellationToken ct
    )
    {
        await _sender.Send(new UpdateUserCommand(id, request), ct);
        return NoContent();
    }

    /// <summary>Activates a previously deactivated user account.</summary>
    [HttpPatch("{id:guid}/activate")]
    [RequirePermission(Permission.Users.Update)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        await _sender.Send(new ActivateUserCommand(id), ct);
        return NoContent();
    }

    /// <summary>Deactivates an active user account, preventing further logins.</summary>
    [HttpPatch("{id:guid}/deactivate")]
    [RequirePermission(Permission.Users.Update)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await _sender.Send(new DeactivateUserCommand(id), ct);
        return NoContent();
    }

    /// <summary>Changes the role of an existing user within the current tenant.</summary>
    [HttpPatch("{id:guid}/role")]
    [RequirePermission(Permission.Users.Update)]
    public async Task<IActionResult> ChangeRole(
        Guid id,
        ChangeUserRoleRequest request,
        CancellationToken ct
    )
    {
        await _sender.Send(new ChangeUserRoleCommand(id, request), ct);
        return NoContent();
    }

    /// <summary>Soft-deletes a user account by its identifier.</summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission(Permission.Users.Delete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _sender.Send(new DeleteUserCommand(id), ct);
        return NoContent();
    }

    /// <summary>
    /// Triggers a Keycloak-initiated password-reset email for the given address; allows
    /// anonymous callers so unauthenticated users can recover access.
    /// </summary>
    [HttpPost("password-reset")]
    [AllowAnonymous]
    public async Task<IActionResult> RequestPasswordReset(
        RequestPasswordResetRequest request,
        CancellationToken ct
    )
    {
        await _sender.Send(new KeycloakPasswordResetCommand(request), ct);
        return Ok();
    }
}
