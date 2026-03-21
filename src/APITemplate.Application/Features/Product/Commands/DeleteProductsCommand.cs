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

        // Load all products with links in a single query
        var distinctIds = ids.Distinct().ToList();
        var products = await _repository.ListAsync(
            new ProductsByIdsWithLinksSpecification(distinctIds),
            ct
        );
        var productMap = products.ToDictionary(p => p.Id);

        var hasFailures = false;

        for (var i = 0; i < ids.Count; i++)
        {
            if (!productMap.ContainsKey(ids[i]))
            {
                results[i] = new BatchResultItem(
                    i,
                    false,
                    ids[i],
                    [$"Product '{ids[i]}' not found."]
                );
                hasFailures = true;
            }
            else
            {
                results[i] = new BatchResultItem(i, true, ids[i], null);
            }
        }

        if (hasFailures)
        {
            var successCount = results.Count(r => r.Success);
            return new BatchResponse(results, successCount, results.Length - successCount);
        }

        // Soft-delete all in a single transaction
        await _unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                foreach (var product in products)
                {
                    product.SoftDeleteProductDataLinks();
                    await _repository.DeleteAsync(product, ct);
                }
            },
            ct
        );

        await _publisher.PublishAsync(new CacheInvalidationNotification(CacheTags.Products), ct);
        await _publisher.PublishAsync(new CacheInvalidationNotification(CacheTags.Reviews), ct);

        return new BatchResponse(results, results.Length, 0);
    }
}
