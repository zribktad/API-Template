using APITemplate.Application.Common.Errors;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Common.Extensions;
using APITemplate.Application.Features.User.DTOs;
using APITemplate.Domain.Entities;
using APITemplate.Domain.Interfaces;
using ErrorOr;
using Wolverine;

namespace APITemplate.Application.Features.User;

public sealed record ChangeUserRoleCommand(Guid Id, ChangeUserRoleRequest Request) : IHasId;

public sealed class ChangeUserRoleCommandHandler
{
    public static async Task<(HandlerContinuation, AppUser?, OutgoingMessages)> LoadAsync(
        ChangeUserRoleCommand command,
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
        ChangeUserRoleCommand command,
        AppUser user,
        IUserRepository repository,
        IUnitOfWork unitOfWork,
        CancellationToken ct
    )
    {
        var oldRole = user.Role.ToString();

        user.Role = command.Request.Role;
        await repository.UpdateAsync(user, ct);
        await unitOfWork.CommitAsync(ct);

        return (
            Result.Success,
            [
                new UserRoleChangedNotification(
                    user.Id,
                    user.Email,
                    user.Username,
                    oldRole,
                    command.Request.Role.ToString()
                ),
                new CacheInvalidationNotification(CacheTags.Users),
            ]
        );
    }
}
