using APITemplate.Application.Common.Batch;
using APITemplate.Application.Common.Batch.Rules;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Features.Product.Specifications;
using ErrorOr;
using Wolverine;
using ProductEntity = APITemplate.Domain.Entities.Product;

namespace APITemplate.Application.Features.Product;

/// <summary>Soft-deletes multiple products and their associated data links in a single batch operation.</summary>
public sealed record DeleteProductsCommand(BatchDeleteRequest Request);

/// <summary>Handles <see cref="DeleteProductsCommand"/> by loading all products, soft-deleting links and products in a single transaction.</summary>
public sealed class DeleteProductsCommandHandler
{
    /// <summary>
    /// Wolverine compound-handler load step: loads products and marks missing ones,
    /// short-circuiting the handler pipeline with a failure response when any ID is not found.
    /// </summary>
    public static async Task<(
        HandlerContinuation,
        List<ProductEntity>?,
        OutgoingMessages
    )> LoadAsync(DeleteProductsCommand command, IProductRepository repository, CancellationToken ct)
    {
        var ids = command.Request.Ids;
        var context = new BatchFailureContext<Guid>(ids);

        // Load all target products and mark missing ones as failed
        var products = await repository.ListAsync(
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

        OutgoingMessages messages = new();

        if (context.HasFailures)
        {
            messages.RespondToSender(context.ToFailureResponse());
            return (HandlerContinuation.Stop, null, messages);
        }

        return (HandlerContinuation.Continue, products, messages);
    }

    /// <summary>Soft-deletes product-data links and removes products in a single transaction.</summary>
    public static async Task<(ErrorOr<BatchResponse>, OutgoingMessages)> HandleAsync(
        DeleteProductsCommand command,
        List<ProductEntity> products,
        IProductRepository repository,
        IUnitOfWork unitOfWork,
        CancellationToken ct
    )
    {
        var ids = command.Request.Ids;

        await unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                foreach (var product in products)
                    product.SoftDeleteProductDataLinks();

                await repository.DeleteRangeAsync(products, ct);
            },
            ct
        );

        return (
            new BatchResponse([], ids.Count, 0),
            [
                new CacheInvalidationNotification(CacheTags.Products),
                new CacheInvalidationNotification(CacheTags.Reviews),
            ]
        );
    }
}
