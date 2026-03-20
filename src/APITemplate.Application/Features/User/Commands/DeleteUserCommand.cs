using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Common.Extensions;
using APITemplate.Application.Common.Security;
using APITemplate.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace APITemplate.Application.Features.User;

public sealed record DeleteUserCommand(Guid Id) : ICommand;

public sealed class DeleteUserCommandHandler : ICommandHandler<DeleteUserCommand>
{
    private readonly IUserRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _publisher;
    private readonly IKeycloakAdminService _keycloakAdmin;
    private readonly ILogger<DeleteUserCommandHandler> _logger;

    public DeleteUserCommandHandler(
        IUserRepository repository,
        IUnitOfWork unitOfWork,
        IEventPublisher publisher,
        IKeycloakAdminService keycloakAdmin,
        ILogger<DeleteUserCommandHandler> logger
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
        _keycloakAdmin = keycloakAdmin;
        _logger = logger;
    }

    public async Task HandleAsync(DeleteUserCommand command, CancellationToken ct)
    {
        var user = await _repository.GetByIdOrThrowAsync(
            command.Id,
            ErrorCatalog.Users.NotFound,
            ct
        );

        if (user.KeycloakUserId is not null)
            await _keycloakAdmin.DeleteUserAsync(user.KeycloakUserId, ct);

        try
        {
            await _repository.DeleteAsync(user, ct);
            await _unitOfWork.CommitAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogCritical(
                ex,
                "DB delete failed after Keycloak user {KeycloakUserId} was already deleted. Manual cleanup required.",
                user.KeycloakUserId
            );
            throw;
        }

        await _publisher.PublishAsync(new CacheInvalidationNotification(CacheTags.Users), ct);
    }
}
