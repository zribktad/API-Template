using Contracts.IntegrationEvents.Identity;
using ErrorOr;
using Identity.Application.Errors;
using Identity.Application.Features.User.DTOs;
using Identity.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using SharedKernel.Application.Context;
using SharedKernel.Application.Extensions;
using SharedKernel.Domain.Entities.Contracts;
using SharedKernel.Domain.Interfaces;
using Wolverine;

namespace Identity.Application.Features.User.Commands;

public sealed record ChangeUserRoleCommand(Guid Id, ChangeUserRoleRequest Request) : IHasId;

public sealed class ChangeUserRoleCommandHandler
{
    public static async Task<ErrorOr<Success>> HandleAsync(
        ChangeUserRoleCommand command,
        IUserRepository repository,
        IUnitOfWork unitOfWork,
        IMessageBus bus,
        ITenantProvider tenantProvider,
        TimeProvider timeProvider,
        ILogger<ChangeUserRoleCommandHandler> logger,
        CancellationToken ct
    )
    {
        ErrorOr<Domain.Entities.AppUser> userResult = await repository.GetByIdOrError(
            command.Id,
            DomainErrors.Users.NotFound(command.Id),
            ct
        );
        if (userResult.IsError)
            return userResult.Errors;
        Domain.Entities.AppUser user = userResult.Value;

        string oldRole = user.Role.ToString();

        user.Role = command.Request.Role;
        await repository.UpdateAsync(user, ct);
        await unitOfWork.CommitAsync(ct);

        try
        {
            await bus.PublishAsync(
                new UserRoleChangedIntegrationEvent(
                    user.Id,
                    tenantProvider.TenantId,
                    user.Email,
                    user.Username,
                    oldRole,
                    command.Request.Role.ToString(),
                    timeProvider.GetUtcNow().UtcDateTime
                )
            );
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                ex,
                "Failed to publish {EventType}.",
                nameof(UserRoleChangedIntegrationEvent)
            );
        }

        return Result.Success;
    }
}
