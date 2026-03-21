using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Features.Product.Specifications;

namespace APITemplate.Application.Features.Product;

/// <summary>Soft-deletes multiple products and their associated data links in a single batch operation.</summary>
public sealed record DeleteProductsCommand(BatchDeleteRequest Request) : ICommand<BatchResponse>;

/// <summary>Handles <see cref="DeleteProductsCommand"/> by loading all products, soft-deleting links and products in a single transaction.</summary>
public sealed class DeleteProductsCommandHandler
    : ICommandHandler<DeleteProductsCommand, BatchResponse>
{
    private readonly IProductRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _publisher;

    public DeleteProductsCommandHandler(
        IProductRepository repository,
        IUnitOfWork unitOfWork,
        IEventPublisher publisher
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task<BatchResponse> HandleAsync(
        DeleteProductsCommand command,
        CancellationToken ct
    )
    {
        var ids = command.Request.Ids;

        // Step 1: Load all target products and mark missing ones as failed
        var products = await _repository.ListAsync(
            new ProductsByIdsWithLinksSpecification(ids.Distinct().ToHashSet()),
            ct
        );

        var results = BatchHelper.Initialize(ids.Count, i => ids[i]);
        var failureCount = BatchHelper.MarkMissing(
            results,
            products.Select(p => p.Id).ToHashSet(),
            ErrorCatalog.Products.NotFoundMessage
        );

        if (failureCount > 0)
            return new BatchResponse(results, results.Length - failureCount, failureCount);

        // Step 2: Soft-delete product-data links and remove products in a single transaction
        await _unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                foreach (var product in products)
                    product.SoftDeleteProductDataLinks();

                await _repository.DeleteRangeAsync(products, ct);
            },
            ct
        );

        await _publisher.PublishAsync(new CacheInvalidationNotification(CacheTags.Products), ct);
        await _publisher.PublishAsync(new CacheInvalidationNotification(CacheTags.Reviews), ct);

        return new BatchResponse(results, results.Length, 0);
    }
}
