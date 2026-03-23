using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Common.Extensions;
using APITemplate.Application.Features.User.DTOs;
using APITemplate.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace APITemplate.Application.Features.User;

public sealed record ChangeUserRoleCommand(Guid Id, ChangeUserRoleRequest Request)
    : ICommand,
        IHasId;

public sealed class ChangeUserRoleCommandHandler : ICommandHandler<ChangeUserRoleCommand>
{
    private readonly IUserRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _publisher;
    private readonly ILogger<ChangeUserRoleCommandHandler> _logger;

    public ChangeUserRoleCommandHandler(
        IUserRepository repository,
        IUnitOfWork unitOfWork,
        IEventPublisher publisher,
        ILogger<ChangeUserRoleCommandHandler> logger
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task HandleAsync(ChangeUserRoleCommand command, CancellationToken ct)
    {
        var user = await _repository.GetByIdOrThrowAsync(
            command.Id,
            ErrorCatalog.Users.NotFound,
            ct
        );
        var oldRole = user.Role.ToString();

        user.Role = command.Request.Role;
        await _repository.UpdateAsync(user, ct);
        await _unitOfWork.CommitAsync(ct);

        await _publisher.PublishSafeAsync(
            new UserRoleChangedNotification(
                user.Id,
                user.Email,
                user.Username,
                oldRole,
                command.Request.Role.ToString()
            ),
            _logger,
            ct
        );

        await _publisher.PublishAsync(new CacheInvalidationNotification(CacheTags.Users), ct);
    }
}
