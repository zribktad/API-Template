using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Common.Extensions;
using APITemplate.Application.Common.Security;
using APITemplate.Domain.Interfaces;

namespace APITemplate.Application.Features.User;

public sealed record DeleteUserCommand(Guid Id) : ICommand;

public sealed class DeleteUserCommandHandler : ICommandHandler<DeleteUserCommand>
{
    private readonly IUserRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _publisher;
    private readonly IKeycloakAdminService _keycloakAdmin;

    public DeleteUserCommandHandler(
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

    public async Task HandleAsync(DeleteUserCommand command, CancellationToken ct)
    {
        var user = await _repository.GetByIdOrThrowAsync(
            command.Id,
            ErrorCatalog.Users.NotFound,
            ct
        );

        if (user.KeycloakUserId is not null)
            await _keycloakAdmin.DeleteUserAsync(user.KeycloakUserId, ct);

        await _repository.DeleteAsync(user, ct);
        await _unitOfWork.CommitAsync(ct);

        await _publisher.PublishAsync(new CacheInvalidationNotification(CacheTags.Users), ct);
    }
}
