using APITemplate.Domain.Entities;
using APITemplate.Domain.Exceptions;
using APITemplate.Domain.Interfaces;

namespace APITemplate.Application.Features.User;

internal static class UserValidationHelper
{
    internal static async Task ValidateEmailUniqueAsync(
        IUserRepository repository,
        string email,
        CancellationToken ct
    )
    {
        if (await repository.ExistsByEmailAsync(email, ct))
        {
            throw new ConflictException(
                $"A user with email '{email}' already exists.",
                ErrorCatalog.Users.EmailAlreadyExists
            );
        }
    }

    internal static async Task ValidateUsernameUniqueAsync(
        IUserRepository repository,
        string username,
        CancellationToken ct
    )
    {
        var normalized = AppUser.NormalizeUsername(username);
        if (await repository.ExistsByUsernameAsync(normalized, ct))
        {
            throw new ConflictException(
                $"A user with username '{username}' already exists.",
                ErrorCatalog.Users.UsernameAlreadyExists
            );
        }
    }
}
