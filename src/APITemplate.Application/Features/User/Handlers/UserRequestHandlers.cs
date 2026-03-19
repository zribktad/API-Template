using APITemplate.Application.Common.Events;
using APITemplate.Application.Common.Security;
using APITemplate.Application.Features.User.DTOs;
using APITemplate.Application.Features.User.Mappings;
using APITemplate.Application.Features.User.Specifications;
using APITemplate.Domain.Entities;
using APITemplate.Domain.Exceptions;
using APITemplate.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace APITemplate.Application.Features.User;

/// <summary>
/// MediatR query that retrieves a paginated, filtered list of users.
/// </summary>
public sealed record GetUsersQuery(UserFilter Filter) : IRequest<PagedResponse<UserResponse>>;

/// <summary>
/// MediatR query that retrieves a single user by their unique identifier.
/// </summary>
public sealed record GetUserByIdQuery(Guid Id) : IRequest<UserResponse?>;

/// <summary>
/// MediatR command that creates a new user account in both Keycloak and the local database.
/// </summary>
public sealed record CreateUserCommand(CreateUserRequest Request) : IRequest<UserResponse>;

/// <summary>
/// MediatR command that updates the username and email of the specified user.
/// </summary>
public sealed record UpdateUserCommand(Guid Id, UpdateUserRequest Request) : IRequest;

/// <summary>
/// MediatR command that activates the specified user account in both Keycloak and the local database.
/// </summary>
public sealed record ActivateUserCommand(Guid Id) : IRequest;

/// <summary>
/// MediatR command that deactivates the specified user account in both Keycloak and the local database.
/// </summary>
public sealed record DeactivateUserCommand(Guid Id) : IRequest;

/// <summary>
/// MediatR command that changes the application role of the specified user.
/// </summary>
public sealed record ChangeUserRoleCommand(Guid Id, ChangeUserRoleRequest Request) : IRequest;

/// <summary>
/// MediatR command that deletes the specified user from Keycloak and the local database.
/// </summary>
public sealed record DeleteUserCommand(Guid Id) : IRequest;

/// <summary>
/// MediatR command that triggers a Keycloak password-reset email for the given email address.
/// </summary>
public sealed record KeycloakPasswordResetCommand(RequestPasswordResetRequest Request) : IRequest;

/// <summary>
/// Application-layer handler that processes all user-related MediatR requests and commands.
/// Coordinates repository access, Keycloak administration, unit-of-work commits, and domain-event publication.
/// </summary>
public sealed class UserRequestHandlers
    : IRequestHandler<GetUsersQuery, PagedResponse<UserResponse>>,
        IRequestHandler<GetUserByIdQuery, UserResponse?>,
        IRequestHandler<CreateUserCommand, UserResponse>,
        IRequestHandler<UpdateUserCommand>,
        IRequestHandler<ActivateUserCommand>,
        IRequestHandler<DeactivateUserCommand>,
        IRequestHandler<ChangeUserRoleCommand>,
        IRequestHandler<DeleteUserCommand>,
        IRequestHandler<KeycloakPasswordResetCommand>
{
    private readonly IUserRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;
    private readonly ILogger<UserRequestHandlers> _logger;
    private readonly IKeycloakAdminService _keycloakAdmin;

    public UserRequestHandlers(
        IUserRepository repository,
        IUnitOfWork unitOfWork,
        IPublisher publisher,
        ILogger<UserRequestHandlers> logger,
        IKeycloakAdminService keycloakAdmin
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
        _logger = logger;
        _keycloakAdmin = keycloakAdmin;
    }

    /// <summary>
    /// Returns a paginated list of users that match the filter criteria. Item and count queries run concurrently.
    /// </summary>
    public async Task<PagedResponse<UserResponse>> Handle(
        GetUsersQuery request,
        CancellationToken ct
    )
    {
        var itemsTask = _repository.ListAsync(new UserFilterSpecification(request.Filter), ct);
        var countTask = _repository.CountAsync(new UserCountSpecification(request.Filter), ct);

        return new PagedResponse<UserResponse>(
            await itemsTask,
            await countTask,
            request.Filter.PageNumber,
            request.Filter.PageSize
        );
    }

    /// <summary>
    /// Returns the user with the specified ID projected to <see cref="UserResponse"/>, or <see langword="null"/> if not found.
    /// </summary>
    public async Task<UserResponse?> Handle(GetUserByIdQuery request, CancellationToken ct) =>
        await _repository.FirstOrDefaultAsync(new UserByIdSpecification(request.Id), ct);

    /// <summary>
    /// Creates the user in Keycloak first, then persists to the local database.
    /// On DB failure a compensating Keycloak delete is attempted and the original exception is re-thrown.
    /// </summary>
    public async Task<UserResponse> Handle(CreateUserCommand command, CancellationToken ct)
    {
        await Task.WhenAll(
            ValidateEmailUniqueAsync(command.Request.Email, ct),
            ValidateUsernameUniqueAsync(command.Request.Username, ct)
        );

        var keycloakUserId = await _keycloakAdmin.CreateUserAsync(
            command.Request.Username,
            command.Request.Email,
            ct
        );

        try
        {
            var user = new AppUser
            {
                Id = Guid.NewGuid(),
                Username = command.Request.Username,
                Email = command.Request.Email,
                KeycloakUserId = keycloakUserId,
            };

            await _repository.AddAsync(user, ct);
            await _unitOfWork.CommitAsync(ct);

            try
            {
                await _publisher.Publish(
                    new UserRegisteredNotification(user.Id, user.Email, user.Username),
                    ct
                );
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to publish UserRegisteredNotification for user {UserId}.",
                    user.Id
                );
            }

            await _publisher.Publish(new UsersChangedNotification(), ct);
            return user.ToResponse();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "DB save failed after creating Keycloak user {KeycloakUserId}. Attempting compensating delete.",
                keycloakUserId
            );
            try
            {
                await _keycloakAdmin.DeleteUserAsync(keycloakUserId, CancellationToken.None);
            }
            catch (Exception compensationEx)
            {
                _logger.LogError(
                    compensationEx,
                    "Compensating Keycloak delete failed for user {KeycloakUserId}. Manual cleanup required.",
                    keycloakUserId
                );
            }
            throw;
        }
    }

    /// <summary>
    /// Updates the user's username and email, validating uniqueness only when the values change.
    /// </summary>
    public async Task Handle(UpdateUserCommand command, CancellationToken ct)
    {
        var user = await GetUserOrThrowAsync(command.Id, ct);

        if (!string.Equals(user.Email, command.Request.Email, StringComparison.OrdinalIgnoreCase))
            await ValidateEmailUniqueAsync(command.Request.Email, ct);

        var normalizedNew = AppUser.NormalizeUsername(command.Request.Username);
        if (!string.Equals(user.NormalizedUsername, normalizedNew, StringComparison.Ordinal))
            await ValidateUsernameUniqueAsync(command.Request.Username, ct);

        user.Username = command.Request.Username;
        user.Email = command.Request.Email;

        await _repository.UpdateAsync(user, ct);
        await _unitOfWork.CommitAsync(ct);

        await _publisher.Publish(new UsersChangedNotification(), ct);
    }

    /// <summary>
    /// Enables the user in Keycloak (when a Keycloak ID is present) and sets <c>IsActive = true</c> locally.
    /// </summary>
    public async Task Handle(ActivateUserCommand command, CancellationToken ct)
    {
        var user = await GetUserOrThrowAsync(command.Id, ct);

        if (user.KeycloakUserId is not null)
            await _keycloakAdmin.SetUserEnabledAsync(user.KeycloakUserId, true, ct);

        user.IsActive = true;
        await _repository.UpdateAsync(user, ct);
        await _unitOfWork.CommitAsync(ct);

        await _publisher.Publish(new UsersChangedNotification(), ct);
    }

    /// <summary>
    /// Disables the user in Keycloak (when a Keycloak ID is present) and sets <c>IsActive = false</c> locally.
    /// </summary>
    public async Task Handle(DeactivateUserCommand command, CancellationToken ct)
    {
        var user = await GetUserOrThrowAsync(command.Id, ct);

        if (user.KeycloakUserId is not null)
            await _keycloakAdmin.SetUserEnabledAsync(user.KeycloakUserId, false, ct);

        user.IsActive = false;
        await _repository.UpdateAsync(user, ct);
        await _unitOfWork.CommitAsync(ct);

        await _publisher.Publish(new UsersChangedNotification(), ct);
    }

    /// <summary>
    /// Updates the user's role and publishes a <c>UserRoleChangedNotification</c>; notification failures are swallowed and logged.
    /// </summary>
    public async Task Handle(ChangeUserRoleCommand command, CancellationToken ct)
    {
        var user = await GetUserOrThrowAsync(command.Id, ct);
        var oldRole = user.Role.ToString();

        user.Role = command.Request.Role;
        await _repository.UpdateAsync(user, ct);
        await _unitOfWork.CommitAsync(ct);

        try
        {
            await _publisher.Publish(
                new UserRoleChangedNotification(
                    user.Id,
                    user.Email,
                    user.Username,
                    oldRole,
                    command.Request.Role.ToString()
                ),
                ct
            );
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(
                ex,
                "Failed to publish UserRoleChangedNotification for user {UserId}.",
                user.Id
            );
        }

        await _publisher.Publish(new UsersChangedNotification(), ct);
    }

    /// <summary>
    /// Deletes the user from Keycloak first, then removes the local record. A 404 from Keycloak on retry is tolerated by the service.
    /// </summary>
    public async Task Handle(DeleteUserCommand command, CancellationToken ct)
    {
        var user = await GetUserOrThrowAsync(command.Id, ct);

        // Keycloak-first delete: if this succeeds but CommitAsync fails, the user no longer
        // exists in Keycloak. On retry, DeleteUserAsync should tolerate a 404 (already deleted).
        // See KeycloakAdminService.DeleteUserAsync — it propagates HttpRequestException for non-2xx.
        if (user.KeycloakUserId is not null)
            await _keycloakAdmin.DeleteUserAsync(user.KeycloakUserId, ct);

        await _repository.DeleteAsync(user, ct);
        await _unitOfWork.CommitAsync(ct);

        await _publisher.Publish(new UsersChangedNotification(), ct);
    }

    /// <summary>
    /// Sends a Keycloak password-reset email if the user exists and has a linked Keycloak account.
    /// Silently no-ops for unknown addresses to avoid user enumeration; failures are swallowed and logged.
    /// </summary>
    public async Task Handle(KeycloakPasswordResetCommand command, CancellationToken ct)
    {
        var user = await _repository.FindByEmailAsync(command.Request.Email, ct);

        if (user is null || user.KeycloakUserId is null)
            return;

        try
        {
            await _keycloakAdmin.SendPasswordResetEmailAsync(user.KeycloakUserId, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(
                ex,
                "Failed to send password reset email for user {UserId}.",
                user.Id
            );
        }
    }

    /// <summary>
    /// Fetches the <see cref="AppUser"/> with the given <paramref name="id"/> or throws <see cref="Domain.Exceptions.NotFoundException"/>.
    /// </summary>
    private async Task<AppUser> GetUserOrThrowAsync(Guid id, CancellationToken ct)
    {
        return await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(AppUser), id, ErrorCatalog.Users.NotFound);
    }

    /// <summary>
    /// Throws <see cref="Domain.Exceptions.ConflictException"/> if the email address is already in use.
    /// </summary>
    private async Task ValidateEmailUniqueAsync(string email, CancellationToken ct)
    {
        if (await _repository.ExistsByEmailAsync(email, ct))
        {
            throw new ConflictException(
                $"A user with email '{email}' already exists.",
                ErrorCatalog.Users.EmailAlreadyExists
            );
        }
    }

    /// <summary>
    /// Throws <see cref="Domain.Exceptions.ConflictException"/> if the normalised username is already in use.
    /// </summary>
    private async Task ValidateUsernameUniqueAsync(string username, CancellationToken ct)
    {
        var normalized = AppUser.NormalizeUsername(username);
        if (await _repository.ExistsByUsernameAsync(normalized, ct))
        {
            throw new ConflictException(
                $"A user with username '{username}' already exists.",
                ErrorCatalog.Users.UsernameAlreadyExists
            );
        }
    }
}
