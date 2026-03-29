using APITemplate.Application.Common.Batch;
using APITemplate.Application.Common.Batch.Rules;
using APITemplate.Application.Features.Product.Specifications;
using ErrorOr;
using SharedKernel.Application.Common.Events;
using Wolverine;

namespace APITemplate.Application.Features.Product;

/// <summary>Soft-deletes multiple products and their associated data links in a single batch operation.</summary>
public sealed record DeleteProductsCommand(BatchDeleteRequest Request);

/// <summary>Handles <see cref="DeleteProductsCommand"/> by loading all products, soft-deleting links and products in a single transaction.</summary>
public sealed class DeleteProductsCommandHandler
{
    public static async Task<(ErrorOr<BatchResponse>, OutgoingMessages)> HandleAsync(
        DeleteProductsCommand command,
        IProductRepository repository,
        IUnitOfWork unitOfWork,
        CancellationToken ct
    )
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

        if (context.HasFailures)
            return (context.ToFailureResponse(), CacheInvalidationCascades.None);

        // Soft-delete product-data links and remove products in a single transaction
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
            CacheInvalidationCascades.ForTags(CacheTags.Products, CacheTags.Reviews)
        );
    }
}
