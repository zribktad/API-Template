using APITemplate.Application.Common.Context;
using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Common.Events;
using APITemplate.Domain.Exceptions;
using APITemplate.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace APITemplate.Application.Features.Tenant;

public sealed record DeleteTenantCommand(Guid Id) : ICommand;

public sealed class DeleteTenantCommandHandler : ICommandHandler<DeleteTenantCommand>
{
    private readonly ITenantRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _publisher;
    private readonly IActorProvider _actorProvider;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DeleteTenantCommandHandler> _logger;

    public DeleteTenantCommandHandler(
        ITenantRepository repository,
        IUnitOfWork unitOfWork,
        IEventPublisher publisher,
        IActorProvider actorProvider,
        TimeProvider timeProvider,
        ILogger<DeleteTenantCommandHandler> logger
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
        _actorProvider = actorProvider;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task HandleAsync(DeleteTenantCommand command, CancellationToken ct)
    {
        await _unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                await _repository.DeleteAsync(command.Id, ct, ErrorCatalog.Tenants.NotFound);
            },
            ct
        );

        await _publisher.PublishSafeAsync(
            new TenantSoftDeletedNotification(
                command.Id,
                _actorProvider.ActorId,
                _timeProvider.GetUtcNow().UtcDateTime
            ),
            _logger,
            ct
        );

        await _publisher.PublishAsync(new CacheInvalidationNotification(CacheTags.Tenants), ct);
    }
}
