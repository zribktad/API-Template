using Contracts.IntegrationEvents.Identity;
using ErrorOr;
using Identity.Application.Errors;
using Identity.Application.Features.User.DTOs;
using Identity.Application.Features.User.Mappings;
using Identity.Application.Security;
using Identity.Domain.Entities;
using Identity.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using SharedKernel.Application.Context;
using SharedKernel.Domain.Interfaces;
using Wolverine;

namespace Identity.Application.Features.User.Commands;

public sealed record CreateUserCommand(CreateUserRequest Request);

public sealed class CreateUserCommandHandler
{
    public static async Task<ErrorOr<UserResponse>> HandleAsync(
        CreateUserCommand command,
        IUserRepository repository,
        IUnitOfWork unitOfWork,
        IMessageBus bus,
        ILogger<CreateUserCommandHandler> logger,
        IKeycloakAdminService keycloakAdmin,
        ITenantProvider tenantProvider,
        TimeProvider timeProvider,
        CancellationToken ct
    )
    {
        ErrorOr<Success> emailResult = await UserValidationHelper.ValidateEmailUniqueAsync(
            repository,
            command.Request.Email,
            ct
        );
        if (emailResult.IsError)
            return emailResult.Errors;

        ErrorOr<Success> usernameResult = await UserValidationHelper.ValidateUsernameUniqueAsync(
            repository,
            command.Request.Username,
            ct
        );
        if (usernameResult.IsError)
            return usernameResult.Errors;

        string keycloakUserId = await keycloakAdmin.CreateUserAsync(
            command.Request.Username,
            command.Request.Email,
            ct
        );

        try
        {
            AppUser user = new()
            {
                Id = Guid.NewGuid(),
                Username = command.Request.Username,
                Email = command.Request.Email,
                KeycloakUserId = keycloakUserId,
            };

            await repository.AddAsync(user, ct);
            await unitOfWork.CommitAsync(ct);

            try
            {
                await bus.PublishAsync(
                    new UserRegisteredIntegrationEvent(
                        user.Id,
                        tenantProvider.TenantId,
                        user.Email,
                        user.Username,
                        timeProvider.GetUtcNow().UtcDateTime
                    )
                );
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(
                    ex,
                    "Failed to publish {EventType}.",
                    nameof(UserRegisteredIntegrationEvent)
                );
            }

            return user.ToResponse();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(
                ex,
                "DB save failed after creating Keycloak user {KeycloakUserId}. Attempting compensating delete.",
                keycloakUserId
            );
            try
            {
                await keycloakAdmin.DeleteUserAsync(keycloakUserId, CancellationToken.None);
            }
            catch (Exception compensationEx)
            {
                logger.LogError(
                    compensationEx,
                    "Compensating Keycloak delete failed for user {KeycloakUserId}. Manual cleanup required.",
                    keycloakUserId
                );
            }
            throw;
        }
    }
}
