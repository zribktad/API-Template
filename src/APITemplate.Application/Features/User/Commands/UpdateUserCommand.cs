using APITemplate.Application.Common.Errors;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Common.Extensions;
using APITemplate.Application.Features.User.DTOs;
using APITemplate.Domain.Entities;
using APITemplate.Domain.Interfaces;
using ErrorOr;
using Wolverine;

namespace APITemplate.Application.Features.User;

/// <summary>Updates user profile (email, username) with uniqueness validation.</summary>
public sealed record UpdateUserCommand(Guid Id, UpdateUserRequest Request) : IHasId;

public sealed class UpdateUserCommandHandler
{
    public static async Task<(HandlerContinuation, AppUser?, OutgoingMessages)> LoadAsync(
        UpdateUserCommand command,
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

        var user = userResult.Value;

        if (!string.Equals(user.Email, command.Request.Email, StringComparison.OrdinalIgnoreCase))
        {
            var emailResult = await UserValidationHelper.ValidateEmailUniqueAsync(
                repository,
                command.Request.Email,
                ct
            );
            if (emailResult.IsError)
            {
                messages.RespondToSender((ErrorOr<Success>)emailResult.Errors);
                return (HandlerContinuation.Stop, null, messages);
            }
        }

        var normalizedNew = AppUser.NormalizeUsername(command.Request.Username);
        if (!string.Equals(user.NormalizedUsername, normalizedNew, StringComparison.Ordinal))
        {
            var usernameResult = await UserValidationHelper.ValidateUsernameUniqueAsync(
                repository,
                command.Request.Username,
                ct
            );
            if (usernameResult.IsError)
            {
                messages.RespondToSender((ErrorOr<Success>)usernameResult.Errors);
                return (HandlerContinuation.Stop, null, messages);
            }
        }

        return (HandlerContinuation.Continue, user, messages);
    }

    public static async Task<(ErrorOr<Success>, OutgoingMessages)> HandleAsync(
        UpdateUserCommand command,
        AppUser user,
        IUserRepository repository,
        IUnitOfWork unitOfWork,
        CancellationToken ct
    )
    {
        user.Username = command.Request.Username;
        user.Email = command.Request.Email;

        await repository.UpdateAsync(user, ct);
        await unitOfWork.CommitAsync(ct);

        return (Result.Success, [new CacheInvalidationNotification(CacheTags.Users)]);
    }
}
