using APITemplate.Application.Common.Events;
using APITemplate.Application.Common.Extensions;
using APITemplate.Application.Common.Security;
using APITemplate.Domain.Interfaces;
using Wolverine;

namespace APITemplate.Application.Features.User;

public sealed record SetUserActiveCommand(Guid Id, bool IsActive) : IHasId;

public sealed class SetUserActiveCommandHandler
{
    public static async Task HandleAsync(
        SetUserActiveCommand command,
        IUserRepository repository,
        IUnitOfWork unitOfWork,
        IMessageBus bus,
        IKeycloakAdminService keycloakAdmin,
        CancellationToken ct
    )
    {
        var user = await repository.GetByIdOrThrowAsync(
            command.Id,
            ErrorCatalog.Users.NotFound,
            ct
        );

        if (user.KeycloakUserId is not null)
            await keycloakAdmin.SetUserEnabledAsync(user.KeycloakUserId, command.IsActive, ct);

        user.IsActive = command.IsActive;
        await repository.UpdateAsync(user, ct);
        await unitOfWork.CommitAsync(ct);

        await bus.PublishAsync(new CacheInvalidationNotification(CacheTags.Users));
    }
}
