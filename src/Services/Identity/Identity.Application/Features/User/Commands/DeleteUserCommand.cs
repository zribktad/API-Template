using ErrorOr;
using Identity.Application.Errors;
using Identity.Application.Security;
using Identity.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using SharedKernel.Application.Extensions;
using SharedKernel.Domain.Entities.Contracts;
using SharedKernel.Domain.Interfaces;
using Wolverine;

namespace Identity.Application.Features.User.Commands;

public sealed record DeleteUserCommand(Guid Id) : IHasId;

public sealed class DeleteUserCommandHandler
{
    public static async Task<ErrorOr<Success>> HandleAsync(
        DeleteUserCommand command,
        IUserRepository repository,
        IUnitOfWork unitOfWork,
        IKeycloakAdminService keycloakAdmin,
        ILogger<DeleteUserCommandHandler> logger,
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

        if (user.KeycloakUserId is not null)
            await keycloakAdmin.DeleteUserAsync(user.KeycloakUserId, ct);

        try
        {
            await repository.DeleteAsync(user, ct);
            await unitOfWork.CommitAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogCritical(
                ex,
                "DB delete failed after Keycloak user {KeycloakUserId} was already deleted. Manual cleanup required.",
                user.KeycloakUserId
            );
            throw;
        }

        return Result.Success;
    }
}
