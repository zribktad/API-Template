using APITemplate.Application.Common.Context;
using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Common.Resilience;
using APITemplate.Domain.Exceptions;
using APITemplate.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Polly.Registry;

namespace APITemplate.Application.Features.ProductData;

public sealed record DeleteProductDataCommand(Guid Id) : ICommand;

public sealed class DeleteProductDataCommandHandler : ICommandHandler<DeleteProductDataCommand>
{
    private readonly IProductDataRepository _repository;
    private readonly IProductDataLinkRepository _productDataLinkRepository;
    private readonly ITenantProvider _tenantProvider;
    private readonly IActorProvider _actorProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _publisher;
    private readonly TimeProvider _timeProvider;
    private readonly ResiliencePipelineProvider<string> _resiliencePipelineProvider;
    private readonly ILogger<DeleteProductDataCommandHandler> _logger;

    public DeleteProductDataCommandHandler(
        IProductDataRepository repository,
        IProductDataLinkRepository productDataLinkRepository,
        ITenantProvider tenantProvider,
        IActorProvider actorProvider,
        IUnitOfWork unitOfWork,
        IEventPublisher publisher,
        TimeProvider timeProvider,
        ResiliencePipelineProvider<string> resiliencePipelineProvider,
        ILogger<DeleteProductDataCommandHandler> logger
    )
    {
        _repository = repository;
        _productDataLinkRepository = productDataLinkRepository;
        _tenantProvider = tenantProvider;
        _actorProvider = actorProvider;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
        _timeProvider = timeProvider;
        _resiliencePipelineProvider = resiliencePipelineProvider;
        _logger = logger;
    }

    public async Task HandleAsync(DeleteProductDataCommand command, CancellationToken ct)
    {
        var tenantId = _tenantProvider.TenantId;

        var data =
            await _repository.GetByIdAsync(command.Id, ct)
            ?? throw new NotFoundException(
                nameof(Domain.Entities.ProductData),
                command.Id,
                ErrorCatalog.ProductData.NotFound
            );

        if (data.TenantId != tenantId)
            throw new NotFoundException(
                nameof(Domain.Entities.ProductData),
                command.Id,
                ErrorCatalog.ProductData.NotFound
            );

        var deletedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var actorId = _actorProvider.ActorId;

        await _unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                await _productDataLinkRepository.SoftDeleteActiveLinksForProductDataAsync(
                    command.Id,
                    ct
                );
            },
            ct
        );

        var pipeline = _resiliencePipelineProvider.GetPipeline(
            ResiliencePipelineKeys.MongoProductDataDelete
        );

        try
        {
            await pipeline.ExecuteAsync(
                async token =>
                    await _repository.SoftDeleteAsync(data.Id, actorId, deletedAtUtc, token),
                ct
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to soft-delete ProductData document {ProductDataId} for tenant {TenantId}. Related ProductDataLinks may already be soft-deleted in PostgreSQL.",
                data.Id,
                tenantId
            );
            throw;
        }

        await _publisher.PublishAsync(new ProductDataChangedNotification(), ct);
    }
}
