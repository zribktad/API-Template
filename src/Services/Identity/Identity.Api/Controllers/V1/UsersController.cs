using System.Security.Claims;
using Asp.Versioning;
using Identity.Application.Features.User.Commands;
using Identity.Application.Features.User.DTOs;
using Identity.Application.Features.User.Queries;
using Identity.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using SharedKernel.Api.Authorization;
using SharedKernel.Api.Controllers;
using SharedKernel.Api.Extensions;
using SharedKernel.Api.Filters.Idempotency;
using SharedKernel.Application.Common.Events;
using SharedKernel.Application.Security;
using SharedKernel.Domain.Common;
using Wolverine;

namespace Identity.Api.Controllers.V1;

[ApiVersion(1.0)]
public sealed class UsersController(IMessageBus bus) : ApiControllerBase
{
    [HttpGet]
    [RequirePermission(Permission.Users.Read)]
    [OutputCache(PolicyName = CacheTags.Users)]
    public Task<ActionResult<PagedResponse<UserResponse>>> GetAll(
        [FromQuery] UserFilter filter,
        CancellationToken ct
    ) => InvokeToActionResultAsync<PagedResponse<UserResponse>>(bus, new GetUsersQuery(filter), ct);

    [HttpGet("{id:guid}")]
    [RequirePermission(Permission.Users.Read)]
    [OutputCache(PolicyName = CacheTags.Users)]
    public Task<ActionResult<UserResponse>> GetById(Guid id, CancellationToken ct) =>
        InvokeToActionResultAsync<UserResponse>(bus, new GetUserByIdQuery(id), ct);

    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> GetMe(CancellationToken ct)
    {
        string? userId =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(AuthConstants.Claims.Subject);

        if (userId is null || !Guid.TryParse(userId, out Guid id))
            return Unauthorized();

        return await InvokeToActionResultAsync<UserResponse>(bus, new GetUserByIdQuery(id), ct);
    }

    [HttpPost]
    [Idempotent]
    [RequirePermission(Permission.Users.Create)]
    public Task<ActionResult<UserResponse>> Create(
        CreateUserRequest request,
        CancellationToken ct
    ) =>
        InvokeToCreatedResultAsync<UserResponse>(
            bus,
            new CreateUserCommand(request),
            v => new { id = v.Id, version = this.GetApiVersion() },
            ct
        );

    [HttpPut("{id:guid}")]
    [RequirePermission(Permission.Users.Update)]
    public Task<IActionResult> Update(Guid id, UpdateUserRequest request, CancellationToken ct) =>
        InvokeToNoContentResultAsync(bus, new UpdateUserCommand(id, request), ct);

    [HttpPatch("{id:guid}/activate")]
    [RequirePermission(Permission.Users.Update)]
    public Task<IActionResult> Activate(Guid id, CancellationToken ct) =>
        InvokeToNoContentResultAsync(bus, new SetUserActiveCommand(id, IsActive: true), ct);

    [HttpPatch("{id:guid}/deactivate")]
    [RequirePermission(Permission.Users.Update)]
    public Task<IActionResult> Deactivate(Guid id, CancellationToken ct) =>
        InvokeToNoContentResultAsync(bus, new SetUserActiveCommand(id, IsActive: false), ct);

    [HttpPatch("{id:guid}/role")]
    [RequirePermission(Permission.Users.Update)]
    public Task<IActionResult> ChangeRole(
        Guid id,
        ChangeUserRoleRequest request,
        CancellationToken ct
    ) => InvokeToNoContentResultAsync(bus, new ChangeUserRoleCommand(id, request), ct);

    [HttpDelete("{id:guid}")]
    [RequirePermission(Permission.Users.Delete)]
    public Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        InvokeToNoContentResultAsync(bus, new DeleteUserCommand(id), ct);

    [HttpPost("password-reset")]
    [AllowAnonymous]
    public Task<IActionResult> RequestPasswordReset(
        RequestPasswordResetRequest request,
        CancellationToken ct
    ) => InvokeToOkResultAsync(bus, new KeycloakPasswordResetCommand(request), ct);
}
