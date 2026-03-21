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
            new ProductsByIdsWithLinksSpecification(ids.ToHashSet()),
            ct
        );

        var foundIds = products.Select(p => p.Id).ToHashSet();
        var failures = BatchHelper.MarkMissing(
            ids,
            foundIds.Contains,
            ErrorCatalog.Products.NotFoundMessage
        );

        if (failures.Count > 0)
            return BatchHelper.ToAtomicFailureResponse(failures);

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

        return new BatchResponse([], ids.Count, 0);
    }
}
