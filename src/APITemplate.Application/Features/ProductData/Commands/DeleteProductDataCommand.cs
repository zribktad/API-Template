using APITemplate.Application.Common.Context;
using APITemplate.Application.Common.Errors;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Common.Resilience;
using APITemplate.Domain.Interfaces;
using ErrorOr;
using Microsoft.Extensions.Logging;
using Polly.Registry;
using Wolverine;
using ProductDataEntity = APITemplate.Domain.Entities.ProductData.ProductData;

namespace APITemplate.Application.Features.ProductData;

public sealed record DeleteProductDataCommand(Guid Id) : IHasId;

public sealed class DeleteProductDataCommandHandler
{
    /// <summary>
    /// Wolverine compound-handler load step: loads the product-data document and verifies
    /// tenant ownership, short-circuiting the handler pipeline when not found.
    /// </summary>
    public static async Task<(HandlerContinuation, ProductDataEntity?, OutgoingMessages)> LoadAsync(
        DeleteProductDataCommand command,
        IProductDataRepository repository,
        ITenantProvider tenantProvider,
        CancellationToken ct
    )
    {
        var tenantId = tenantProvider.TenantId;
        var data = await repository.GetByIdAsync(command.Id, ct);

        OutgoingMessages messages = new();

        if (data is null || data.TenantId != tenantId)
        {
            messages.RespondToSender(DomainErrors.ProductData.NotFound(command.Id));
            return (HandlerContinuation.Stop, null, messages);
        }

        return (HandlerContinuation.Continue, data, messages);
    }

    /// <summary>Soft-deletes product-data links in PostgreSQL and the document in MongoDB with resilience.</summary>
    public static async Task<(ErrorOr<Success>, OutgoingMessages)> HandleAsync(
        DeleteProductDataCommand command,
        ProductDataEntity data,
        IProductDataRepository repository,
        IProductDataLinkRepository productDataLinkRepository,
        IActorProvider actorProvider,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        ResiliencePipelineProvider<string> resiliencePipelineProvider,
        ILogger<DeleteProductDataCommandHandler> logger,
        CancellationToken ct
    )
    {
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
                data.TenantId
            );
            throw;
        }

        return (Result.Success, [new CacheInvalidationNotification(CacheTags.ProductData)]);
    }
}
