using ErrorOr;
using Identity.Application.Errors;
using Identity.Application.Features.User.DTOs;
using Identity.Domain.Entities;
using Identity.Domain.Interfaces;
using SharedKernel.Application.Extensions;
using SharedKernel.Domain.Entities.Contracts;
using SharedKernel.Domain.Interfaces;

namespace Identity.Application.Features.User.Commands;

public sealed record UpdateUserCommand(Guid Id, UpdateUserRequest Request) : IHasId;

public sealed class UpdateUserCommandHandler
{
    public static async Task<ErrorOr<Success>> HandleAsync(
        UpdateUserCommand command,
        IUserRepository repository,
        IUnitOfWork unitOfWork,
        CancellationToken ct
    )
    {
        ErrorOr<AppUser> userResult = await repository.GetByIdOrError(
            command.Id,
            DomainErrors.Users.NotFound(command.Id),
            ct
        );
        if (userResult.IsError)
            return userResult.Errors;
        AppUser user = userResult.Value;

        if (!string.Equals(user.Email, command.Request.Email, StringComparison.OrdinalIgnoreCase))
        {
            ErrorOr<Success> emailResult = await UserValidationHelper.ValidateEmailUniqueAsync(
                repository,
                command.Request.Email,
                ct
            );
            if (emailResult.IsError)
                return emailResult.Errors;
        }

        string normalizedNew = AppUser.NormalizeUsername(command.Request.Username);
        if (!string.Equals(user.NormalizedUsername, normalizedNew, StringComparison.Ordinal))
        {
            ErrorOr<Success> usernameResult =
                await UserValidationHelper.ValidateUsernameUniqueAsync(
                    repository,
                    command.Request.Username,
                    ct
                );
            if (usernameResult.IsError)
                return usernameResult.Errors;
        }

        user.Username = command.Request.Username;
        user.Email = command.Request.Email;

        await repository.UpdateAsync(user, ct);
        await unitOfWork.CommitAsync(ct);

        return Result.Success;
    }
}
