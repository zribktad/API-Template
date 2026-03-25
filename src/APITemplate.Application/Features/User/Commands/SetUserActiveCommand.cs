using APITemplate.Application.Common.Errors;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Common.Extensions;
using APITemplate.Application.Common.Security;
using APITemplate.Domain.Interfaces;
using ErrorOr;
using Wolverine;

namespace APITemplate.Application.Features.User;

public sealed record SetUserActiveCommand(Guid Id, bool IsActive) : IHasId;

public sealed class SetUserActiveCommandHandler
{
    public static async Task<(ErrorOr<Success>, OutgoingMessages)> HandleAsync(
        SetUserActiveCommand command,
        IUserRepository repository,
        IUnitOfWork unitOfWork,
        IKeycloakAdminService keycloakAdmin,
        CancellationToken ct
    )
    {
        var userResult = await repository.GetByIdOrError(
            command.Id,
            DomainErrors.Users.NotFound(command.Id),
            ct
        );
        if (userResult.IsError)
            return (userResult.Errors, []);
        var user = userResult.Value;

        if (user.KeycloakUserId is not null)
            await keycloakAdmin.SetUserEnabledAsync(user.KeycloakUserId, command.IsActive, ct);

        user.IsActive = command.IsActive;
        await repository.UpdateAsync(user, ct);
        await unitOfWork.CommitAsync(ct);

        return (Result.Success, [new CacheInvalidationNotification(CacheTags.Users)]);
    }
}
