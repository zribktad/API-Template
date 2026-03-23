using APITemplate.Application.Common.Events;
using APITemplate.Application.Common.Extensions;
using APITemplate.Application.Features.User.DTOs;
using APITemplate.Domain.Entities;
using APITemplate.Domain.Interfaces;
using Wolverine;

namespace APITemplate.Application.Features.User;

public sealed record UpdateUserCommand(Guid Id, UpdateUserRequest Request) : IHasId;

public sealed class UpdateUserCommandHandler
{
    public static async Task HandleAsync(
        UpdateUserCommand command,
        IUserRepository repository,
        IUnitOfWork unitOfWork,
        IMessageBus bus,
        CancellationToken ct
    )
    {
        var user = await repository.GetByIdOrThrowAsync(
            command.Id,
            ErrorCatalog.Users.NotFound,
            ct
        );

        if (!string.Equals(user.Email, command.Request.Email, StringComparison.OrdinalIgnoreCase))
            await UserValidationHelper.ValidateEmailUniqueAsync(
                repository,
                command.Request.Email,
                ct
            );

        var normalizedNew = AppUser.NormalizeUsername(command.Request.Username);
        if (!string.Equals(user.NormalizedUsername, normalizedNew, StringComparison.Ordinal))
            await UserValidationHelper.ValidateUsernameUniqueAsync(
                repository,
                command.Request.Username,
                ct
            );

        user.Username = command.Request.Username;
        user.Email = command.Request.Email;

        await repository.UpdateAsync(user, ct);
        await unitOfWork.CommitAsync(ct);

        await bus.PublishAsync(new CacheInvalidationNotification(CacheTags.Users));
    }
}
