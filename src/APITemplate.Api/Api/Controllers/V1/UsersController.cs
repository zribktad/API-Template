using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using APITemplate.Api.Authorization;
using APITemplate.Api.Controllers;
using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Common.DTOs;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Common.Security;
using APITemplate.Application.Features.User.DTOs;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace APITemplate.Api.Controllers.V1;

[ApiVersion(1.0)]
/// <summary>
/// Presentation-layer controller that exposes user management endpoints including
/// CRUD operations, activation/deactivation, role changes, and self-service password reset.
/// </summary>
public sealed class UsersController : ApiControllerBase
{
    /// <summary>Returns a paginated, filterable list of users.</summary>
    [HttpGet]
    [RequirePermission(Permission.Users.Read)]
    [OutputCache(PolicyName = CacheTags.Users)]
    public async Task<ActionResult<PagedResponse<UserResponse>>> GetAll(
        [FromQuery] UserFilter filter,
        [FromServices] IQueryHandler<GetUsersQuery, PagedResponse<UserResponse>> handler,
        CancellationToken ct
    )
    {
        var result = await handler.HandleAsync(new GetUsersQuery(filter), ct);
        return Ok(result);
    }

    /// <summary>Returns a single user by their identifier, or 404 if not found.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(Permission.Users.Read)]
    [OutputCache(PolicyName = CacheTags.Users)]
    public async Task<ActionResult<UserResponse>> GetById(
        Guid id,
        [FromServices] IQueryHandler<GetUserByIdQuery, UserResponse?> handler,
        CancellationToken ct
    )
    {
        var user = await handler.HandleAsync(new GetUserByIdQuery(id), ct);
        return OkOrNotFound(user);
    }

    /// <summary>
    /// Returns the currently authenticated user's profile by resolving their id from the
    /// JWT/cookie claims (<c>NameIdentifier</c>, <c>sub</c>, or a custom subject claim).
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> GetMe(
        [FromServices] IQueryHandler<GetUserByIdQuery, UserResponse?> handler,
        CancellationToken ct
    )
    {
        var userId =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(AuthConstants.Claims.Subject);

        if (userId is null || !Guid.TryParse(userId, out var id))
            return Unauthorized();

        var user = await handler.HandleAsync(new GetUserByIdQuery(id), ct);
        return OkOrNotFound(user);
    }

    /// <summary>Creates a new user account and returns it with a 201 Location header.</summary>
    [HttpPost]
    [RequirePermission(Permission.Users.Create)]
    public async Task<ActionResult<UserResponse>> Create(
        CreateUserRequest request,
        [FromServices] ICommandHandler<CreateUserCommand, UserResponse> handler,
        CancellationToken ct
    )
    {
        var user = await handler.HandleAsync(new CreateUserCommand(request), ct);
        return CreatedAtGetById(user, user.Id);
    }

    /// <summary>Replaces all mutable fields of an existing user.</summary>
    [HttpPut("{id:guid}")]
    [RequirePermission(Permission.Users.Update)]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateUserRequest request,
        [FromServices] ICommandHandler<UpdateUserCommand> handler,
        CancellationToken ct
    )
    {
        await handler.HandleAsync(new UpdateUserCommand(id, request), ct);
        return NoContent();
    }

    /// <summary>Activates a previously deactivated user account.</summary>
    [HttpPatch("{id:guid}/activate")]
    [RequirePermission(Permission.Users.Update)]
    public async Task<IActionResult> Activate(
        Guid id,
        [FromServices] ICommandHandler<ActivateUserCommand> handler,
        CancellationToken ct
    )
    {
        await handler.HandleAsync(new ActivateUserCommand(id), ct);
        return NoContent();
    }

    /// <summary>Deactivates an active user account, preventing further logins.</summary>
    [HttpPatch("{id:guid}/deactivate")]
    [RequirePermission(Permission.Users.Update)]
    public async Task<IActionResult> Deactivate(
        Guid id,
        [FromServices] ICommandHandler<DeactivateUserCommand> handler,
        CancellationToken ct
    )
    {
        await handler.HandleAsync(new DeactivateUserCommand(id), ct);
        return NoContent();
    }

    /// <summary>Changes the role of an existing user within the current tenant.</summary>
    [HttpPatch("{id:guid}/role")]
    [RequirePermission(Permission.Users.Update)]
    public async Task<IActionResult> ChangeRole(
        Guid id,
        ChangeUserRoleRequest request,
        [FromServices] ICommandHandler<ChangeUserRoleCommand> handler,
        CancellationToken ct
    )
    {
        await handler.HandleAsync(new ChangeUserRoleCommand(id, request), ct);
        return NoContent();
    }

    /// <summary>Soft-deletes a user account by its identifier.</summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission(Permission.Users.Delete)]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] ICommandHandler<DeleteUserCommand> handler,
        CancellationToken ct
    )
    {
        await handler.HandleAsync(new DeleteUserCommand(id), ct);
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
        [FromServices] ICommandHandler<KeycloakPasswordResetCommand> handler,
        CancellationToken ct
    )
    {
        await handler.HandleAsync(new KeycloakPasswordResetCommand(request), ct);
        return Ok();
    }
}
