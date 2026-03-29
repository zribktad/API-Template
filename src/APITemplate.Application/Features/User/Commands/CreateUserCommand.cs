using APITemplate.Application.Common.Errors;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Common.Security;
using APITemplate.Application.Features.User.DTOs;
using APITemplate.Application.Features.User.Mappings;
using APITemplate.Domain.Entities;
using APITemplate.Domain.Interfaces;
using ErrorOr;
using Microsoft.Extensions.Logging;
using SharedKernel.Application.Common.Events;
using Wolverine;

namespace APITemplate.Application.Features.User;

public sealed record CreateUserCommand(CreateUserRequest Request);

public sealed class CreateUserCommandHandler
{
    public static async Task<(ErrorOr<UserResponse>, OutgoingMessages)> HandleAsync(
        CreateUserCommand command,
        IUserRepository repository,
        IUnitOfWork unitOfWork,
        IMessageBus bus,
        ILogger<CreateUserCommandHandler> logger,
        IKeycloakAdminService keycloakAdmin,
        CancellationToken ct
    )
    {
        var emailResult = await UserValidationHelper.ValidateEmailUniqueAsync(
            repository,
            command.Request.Email,
            ct
        );
        if (emailResult.IsError)
            return (emailResult.Errors, CacheInvalidationCascades.None);

        var usernameResult = await UserValidationHelper.ValidateUsernameUniqueAsync(
            repository,
            command.Request.Username,
            ct
        );
        if (usernameResult.IsError)
            return (usernameResult.Errors, CacheInvalidationCascades.None);

        var keycloakUserId = await keycloakAdmin.CreateUserAsync(
            command.Request.Username,
            command.Request.Email,
            ct
        );

        try
        {
            var user = new AppUser
            {
                Id = Guid.NewGuid(),
                Username = command.Request.Username,
                Email = command.Request.Email,
                KeycloakUserId = keycloakUserId,
            };

            await repository.AddAsync(user, ct);
            await unitOfWork.CommitAsync(ct);

            await bus.PublishSafeAsync(
                new UserRegisteredNotification(user.Id, user.Email, user.Username),
                logger
            );

            return (user.ToResponse(), CacheInvalidationCascades.ForTag(CacheTags.Users));
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
