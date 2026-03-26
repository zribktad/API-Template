using ErrorOr;
using Identity.Application.Errors;
using Identity.Application.Security;
using Identity.Domain.Interfaces;
using SharedKernel.Application.Extensions;
using SharedKernel.Domain.Entities.Contracts;
using SharedKernel.Domain.Interfaces;

namespace Identity.Application.Features.User.Commands;

public sealed record SetUserActiveCommand(Guid Id, bool IsActive) : IHasId;

public sealed class SetUserActiveCommandHandler
{
    public static async Task<ErrorOr<Success>> HandleAsync(
        SetUserActiveCommand command,
        IUserRepository repository,
        IUnitOfWork unitOfWork,
        IKeycloakAdminService keycloakAdmin,
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
            await keycloakAdmin.SetUserEnabledAsync(user.KeycloakUserId, command.IsActive, ct);

        user.IsActive = command.IsActive;
        await repository.UpdateAsync(user, ct);
        await unitOfWork.CommitAsync(ct);

        return Result.Success;
    }
}
