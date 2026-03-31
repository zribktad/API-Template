using ErrorOr;
using Microsoft.Extensions.Logging;
using Polly.Registry;
using ProductCatalog.Application.Common.Errors;
using ProductCatalog.Application.Common.Resilience;
using ProductCatalog.Domain.Interfaces;
using SharedKernel.Application.Common.Events;
using SharedKernel.Application.Context;
using SharedKernel.Domain.Entities.Contracts;
using SharedKernel.Domain.Interfaces;
using Wolverine;

namespace ProductCatalog.Application.Features.ProductData.Commands;

public sealed record DeleteProductDataCommand(Guid Id) : IHasId;

public sealed class DeleteProductDataCommandHandler
{
    public static async Task<(ErrorOr<Success>, OutgoingMessages)> HandleAsync(
        DeleteProductDataCommand command,
        IProductDataRepository repository,
        IProductDataLinkRepository productDataLinkRepository,
        ITenantProvider tenantProvider,
        IActorProvider actorProvider,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        ResiliencePipelineProvider<string> resiliencePipelineProvider,
        ILogger<DeleteProductDataCommandHandler> logger,
        CancellationToken ct
    )
    {
        Guid tenantId = tenantProvider.TenantId;

        Domain.Entities.ProductData.ProductData? data = await repository.GetByIdAsync(
            command.Id,
            ct
        );

        if (data is null || data.TenantId != tenantId)
            return (DomainErrors.ProductData.NotFound(command.Id), CacheInvalidationCascades.None);

        DateTime deletedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        Guid actorId = actorProvider.ActorId;

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

        Polly.ResiliencePipeline pipeline = resiliencePipelineProvider.GetPipeline(
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

        return (Result.Success, CacheInvalidationCascades.ForTag(CacheTags.ProductData));
    }
}
