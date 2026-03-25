using APITemplate.Application.Common.Errors;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Common.Extensions;
using APITemplate.Application.Common.Security;
using APITemplate.Domain.Entities;
using APITemplate.Domain.Interfaces;
using ErrorOr;
using Wolverine;

namespace APITemplate.Application.Features.User;

public sealed record SetUserActiveCommand(Guid Id, bool IsActive) : IHasId;

public sealed class SetUserActiveCommandHandler
{
    public static async Task<(HandlerContinuation, AppUser?, OutgoingMessages)> LoadAsync(
        SetUserActiveCommand command,
        IUserRepository repository,
        CancellationToken ct
    )
    {
        var userResult = await repository.GetByIdOrError(
            command.Id,
            DomainErrors.Users.NotFound(command.Id),
            ct
        );

        OutgoingMessages messages = new();

        if (userResult.IsError)
        {
            messages.RespondToSender((ErrorOr<Success>)userResult.Errors);
            return (HandlerContinuation.Stop, null, messages);
        }

        return (HandlerContinuation.Continue, userResult.Value, messages);
    }

    public static async Task<(ErrorOr<Success>, OutgoingMessages)> HandleAsync(
        SetUserActiveCommand command,
        AppUser user,
        IUserRepository repository,
        IUnitOfWork unitOfWork,
        IKeycloakAdminService keycloakAdmin,
        CancellationToken ct
    )
    {
        if (user.KeycloakUserId is not null)
            await keycloakAdmin.SetUserEnabledAsync(user.KeycloakUserId, command.IsActive, ct);

        user.IsActive = command.IsActive;
        await repository.UpdateAsync(user, ct);
        await unitOfWork.CommitAsync(ct);

        return (Result.Success, [new CacheInvalidationNotification(CacheTags.Users)]);
    }
}
