using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Common.Extensions;
using APITemplate.Application.Common.Security;
using APITemplate.Domain.Interfaces;

namespace APITemplate.Application.Features.User;

public sealed record ActivateUserCommand(Guid Id) : ICommand;

public sealed record DeactivateUserCommand(Guid Id) : ICommand;

public sealed class ActivateUserCommandHandler : ICommandHandler<ActivateUserCommand>
{
    private readonly SetUserActiveHandler _inner;

    public ActivateUserCommandHandler(
        IUserRepository repository,
        IUnitOfWork unitOfWork,
        IEventPublisher publisher,
        IKeycloakAdminService keycloakAdmin
    ) => _inner = new SetUserActiveHandler(repository, unitOfWork, publisher, keycloakAdmin);

    public Task HandleAsync(ActivateUserCommand command, CancellationToken ct) =>
        _inner.HandleAsync(command.Id, isActive: true, ct);
}

public sealed class DeactivateUserCommandHandler : ICommandHandler<DeactivateUserCommand>
{
    private readonly SetUserActiveHandler _inner;

    public DeactivateUserCommandHandler(
        IUserRepository repository,
        IUnitOfWork unitOfWork,
        IEventPublisher publisher,
        IKeycloakAdminService keycloakAdmin
    ) => _inner = new SetUserActiveHandler(repository, unitOfWork, publisher, keycloakAdmin);

    public Task HandleAsync(DeactivateUserCommand command, CancellationToken ct) =>
        _inner.HandleAsync(command.Id, isActive: false, ct);
}

internal sealed class SetUserActiveHandler
{
    private readonly IUserRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _publisher;
    private readonly IKeycloakAdminService _keycloakAdmin;

    public SetUserActiveHandler(
        IUserRepository repository,
        IUnitOfWork unitOfWork,
        IEventPublisher publisher,
        IKeycloakAdminService keycloakAdmin
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
        _keycloakAdmin = keycloakAdmin;
    }

    public async Task HandleAsync(Guid userId, bool isActive, CancellationToken ct)
    {
        var user = await _repository.GetByIdOrThrowAsync(userId, ErrorCatalog.Users.NotFound, ct);

        if (user.KeycloakUserId is not null)
            await _keycloakAdmin.SetUserEnabledAsync(user.KeycloakUserId, isActive, ct);

        user.IsActive = isActive;
        await _repository.UpdateAsync(user, ct);
        await _unitOfWork.CommitAsync(ct);

        await _publisher.PublishAsync(new CacheInvalidationNotification(CacheTags.Users), ct);
    }
}
