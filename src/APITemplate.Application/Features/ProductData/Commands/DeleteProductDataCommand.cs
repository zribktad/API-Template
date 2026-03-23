using APITemplate.Application.Common.Context;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Common.Resilience;
using APITemplate.Domain.Exceptions;
using APITemplate.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Polly.Registry;
using Wolverine;

namespace APITemplate.Application.Features.ProductData;

public sealed record DeleteProductDataCommand(Guid Id) : IHasId;

public sealed class DeleteProductDataCommandHandler
{
    public static async Task HandleAsync(
        DeleteProductDataCommand command,
        IProductDataRepository repository,
        IProductDataLinkRepository productDataLinkRepository,
        ITenantProvider tenantProvider,
        IActorProvider actorProvider,
        IUnitOfWork unitOfWork,
        IMessageBus bus,
        TimeProvider timeProvider,
        ResiliencePipelineProvider<string> resiliencePipelineProvider,
        ILogger<DeleteProductDataCommandHandler> logger,
        CancellationToken ct
    )
    {
        var tenantId = tenantProvider.TenantId;

        var data =
            await repository.GetByIdAsync(command.Id, ct)
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

        var deletedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        var actorId = actorProvider.ActorId;

        await unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                await productDataLinkRepository.SoftDeleteActiveLinksForProductDataAsync(
                    command.Id,
                    ct
                );
            },
            ct
        );

        var pipeline = resiliencePipelineProvider.GetPipeline(
            ResiliencePipelineKeys.MongoProductDataDelete
        );

        try
        {
            await pipeline.ExecuteAsync(
                async token =>
                    await repository.SoftDeleteAsync(data.Id, actorId, deletedAtUtc, token),
                ct
            );
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to soft-delete ProductData document {ProductDataId} for tenant {TenantId}. Related ProductDataLinks may already be soft-deleted in PostgreSQL.",
                data.Id,
                tenantId
            );
            throw;
        }

        await bus.PublishAsync(new CacheInvalidationNotification(CacheTags.ProductData));
    }
}
