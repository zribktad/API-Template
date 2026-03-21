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
        var results = new BatchResultItem[ids.Count];

        var distinctIds = ids.Distinct().ToHashSet();
        var products = await _repository.ListAsync(
            new ProductsByIdsWithLinksSpecification(distinctIds),
            ct
        );
        var foundIds = products.Select(p => p.Id).ToHashSet();

        for (var i = 0; i < ids.Count; i++)
            results[i] = new BatchResultItem(i, true, ids[i], null);

        var failureCount = BatchHelper.MarkMissing(
            results,
            ids.Count,
            i => ids[i],
            foundIds.Contains,
            ErrorCatalog.Products.NotFoundMessage
        );

        if (failureCount > 0)
            return new BatchResponse(results, results.Length - failureCount, failureCount);

        await _unitOfWork.ExecuteInTransactionAsync(
            () =>
            {
                foreach (var product in products)
                    product.SoftDeleteProductDataLinks();

                return _repository.DeleteRangeAsync(products, ct);
            },
            ct
        );

        await _publisher.PublishAsync(new CacheInvalidationNotification(CacheTags.Products), ct);
        await _publisher.PublishAsync(new CacheInvalidationNotification(CacheTags.Reviews), ct);

        return new BatchResponse(results, results.Length, 0);
    }
}
