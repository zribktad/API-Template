using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Common.Extensions;
using APITemplate.Application.Features.User.DTOs;
using APITemplate.Domain.Entities;
using APITemplate.Domain.Interfaces;

namespace APITemplate.Application.Features.User;

public sealed record UpdateUserCommand(Guid Id, UpdateUserRequest Request) : ICommand;

public sealed class UpdateUserCommandHandler : ICommandHandler<UpdateUserCommand>
{
    private readonly IUserRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _publisher;

    public UpdateUserCommandHandler(
        IUserRepository repository,
        IUnitOfWork unitOfWork,
        IEventPublisher publisher
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task HandleAsync(UpdateUserCommand command, CancellationToken ct)
    {
        var user = await _repository.GetByIdOrThrowAsync(
            command.Id,
            ErrorCatalog.Users.NotFound,
            ct
        );

        if (!string.Equals(user.Email, command.Request.Email, StringComparison.OrdinalIgnoreCase))
            await UserValidationHelper.ValidateEmailUniqueAsync(
                _repository,
                command.Request.Email,
                ct
            );

        var normalizedNew = AppUser.NormalizeUsername(command.Request.Username);
        if (!string.Equals(user.NormalizedUsername, normalizedNew, StringComparison.Ordinal))
            await UserValidationHelper.ValidateUsernameUniqueAsync(
                _repository,
                command.Request.Username,
                ct
            );

        user.Username = command.Request.Username;
        user.Email = command.Request.Email;

        await _repository.UpdateAsync(user, ct);
        await _unitOfWork.CommitAsync(ct);

        await _publisher.PublishAsync(new CacheInvalidationNotification(CacheTags.Users), ct);
    }
}
