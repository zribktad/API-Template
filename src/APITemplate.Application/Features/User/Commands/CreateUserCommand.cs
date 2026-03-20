using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Common.Security;
using APITemplate.Application.Features.User.DTOs;
using APITemplate.Application.Features.User.Mappings;
using APITemplate.Domain.Entities;
using APITemplate.Domain.Exceptions;
using APITemplate.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace APITemplate.Application.Features.User;

public sealed record CreateUserCommand(CreateUserRequest Request) : ICommand<UserResponse>;

public sealed class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, UserResponse>
{
    private readonly IUserRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _publisher;
    private readonly ILogger<CreateUserCommandHandler> _logger;
    private readonly IKeycloakAdminService _keycloakAdmin;

    public CreateUserCommandHandler(
        IUserRepository repository,
        IUnitOfWork unitOfWork,
        IEventPublisher publisher,
        ILogger<CreateUserCommandHandler> logger,
        IKeycloakAdminService keycloakAdmin
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
        _logger = logger;
        _keycloakAdmin = keycloakAdmin;
    }

    public async Task<UserResponse> HandleAsync(CreateUserCommand command, CancellationToken ct)
    {
        await Task.WhenAll(
            UserValidationHelper.ValidateEmailUniqueAsync(_repository, command.Request.Email, ct),
            UserValidationHelper.ValidateUsernameUniqueAsync(
                _repository,
                command.Request.Username,
                ct
            )
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

            await _publisher.PublishSafeAsync(
                new UserRegisteredNotification(user.Id, user.Email, user.Username),
                _logger,
                ct
            );

            await _publisher.PublishAsync(new CacheInvalidationNotification(CacheTags.Users), ct);
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
}
