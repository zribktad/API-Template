using Contracts.IntegrationEvents.Sagas;
using ErrorOr;
using ProductCatalog.Application.Common.Errors;
using ProductCatalog.Application.Features.Product.Repositories;
using ProductCatalog.Application.Features.Product.Specifications;
using SharedKernel.Application.Batch;
using SharedKernel.Application.Batch.Rules;
using SharedKernel.Application.Common.Events;
using SharedKernel.Application.Context;
using SharedKernel.Application.DTOs;
using SharedKernel.Domain.Interfaces;
using Wolverine;

namespace ProductCatalog.Application.Features.Product.Commands;

/// <summary>Soft-deletes multiple products and their associated data links in a single batch operation.</summary>
public sealed record DeleteProductsCommand(BatchDeleteRequest Request);

/// <summary>Handles <see cref="DeleteProductsCommand"/> by loading all products, soft-deleting links and products in a single transaction.</summary>
public sealed class DeleteProductsCommandHandler
{
    public static async Task<(ErrorOr<BatchResponse>, OutgoingMessages)> HandleAsync(
        DeleteProductsCommand command,
        IProductRepository repository,
        IUnitOfWork unitOfWork,
        IMessageBus bus,
        IActorProvider actorProvider,
        CancellationToken ct
    )
    {
        IReadOnlyList<Guid> ids = command.Request.Ids;
        BatchFailureContext<Guid> context = new(ids);

        // Load all target products and mark missing ones as failed
        List<Domain.Entities.Product> products = await repository.ListAsync(
            new ProductsByIdsWithLinksSpecification(ids.ToHashSet()),
            ct
        );

        await context.ApplyRulesAsync(
            ct,
            new MarkMissingByIdBatchRule<Guid>(
                id => id,
                products.Select(product => product.Id).ToHashSet(),
                ErrorCatalog.Products.NotFoundMessage
            )
        );

        if (context.HasFailures)
            return (context.ToFailureResponse(), CacheInvalidationCascades.None);

        Guid tenantId = products[0].TenantId;
        Guid correlationId = Guid.NewGuid();

        // Soft-delete product-data links and remove products in a single transaction
        await unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                foreach (Domain.Entities.Product product in products)
                    product.SoftDeleteProductDataLinks();

                await repository.DeleteRangeAsync(products, ct);
                await bus.PublishAsync(
                    new StartProductDeletionSaga(
                        correlationId,
                        ids,
                        tenantId,
                        actorProvider.ActorId
                    )
                );
            },
            ct
        );

        return (
            new BatchResponse([], ids.Count, 0),
            CacheInvalidationCascades.ForTags(CacheTags.Products, CacheTags.ProductData)
        );
    }
}
