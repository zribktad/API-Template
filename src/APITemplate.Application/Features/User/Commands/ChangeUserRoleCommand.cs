using APITemplate.Application.Common.Events;
using APITemplate.Application.Common.Extensions;
using APITemplate.Application.Features.User.DTOs;
using APITemplate.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace APITemplate.Application.Features.User;

public sealed record ChangeUserRoleCommand(Guid Id, ChangeUserRoleRequest Request) : IHasId;

public sealed class ChangeUserRoleCommandHandler
{
    public static async Task HandleAsync(
        ChangeUserRoleCommand command,
        IUserRepository repository,
        IUnitOfWork unitOfWork,
        IMessageBus bus,
        ILogger<ChangeUserRoleCommandHandler> logger,
        CancellationToken ct
    )
    {
        var user = await repository.GetByIdOrThrowAsync(
            command.Id,
            ErrorCatalog.Users.NotFound,
            ct
        );
        var oldRole = user.Role.ToString();

        user.Role = command.Request.Role;
        await repository.UpdateAsync(user, ct);
        await unitOfWork.CommitAsync(ct);

        await bus.PublishSafeAsync(
            new UserRoleChangedNotification(
                user.Id,
                user.Email,
                user.Username,
                oldRole,
                command.Request.Role.ToString()
            ),
            logger
        );

        await bus.PublishAsync(new CacheInvalidationNotification(CacheTags.Users));
    }
}
