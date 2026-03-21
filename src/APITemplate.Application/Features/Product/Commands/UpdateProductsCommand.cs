using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Features.Product.Specifications;
using FluentValidation;

namespace APITemplate.Application.Features.Product;

/// <summary>Updates multiple products in a single batch operation.</summary>
public sealed record UpdateProductsCommand(UpdateProductsRequest Request) : ICommand<BatchResponse>;

/// <summary>Handles <see cref="UpdateProductsCommand"/> by validating all items, loading products in bulk, and updating in a single transaction.</summary>
public sealed class UpdateProductsCommandHandler
    : ICommandHandler<UpdateProductsCommand, BatchResponse>
{
    private readonly IProductRepository _repository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IProductDataRepository _productDataRepository;
    private readonly IProductDataLinkRepository _productDataLinkRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _publisher;
    private readonly IValidator<UpdateProductItem> _itemValidator;

    public UpdateProductsCommandHandler(
        IProductRepository repository,
        ICategoryRepository categoryRepository,
        IProductDataRepository productDataRepository,
        IProductDataLinkRepository productDataLinkRepository,
        IUnitOfWork unitOfWork,
        IEventPublisher publisher,
        IValidator<UpdateProductItem> itemValidator
    )
    {
        _repository = repository;
        _categoryRepository = categoryRepository;
        _productDataRepository = productDataRepository;
        _productDataLinkRepository = productDataLinkRepository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
        _itemValidator = itemValidator;
    }

    public async Task<BatchResponse> HandleAsync(
        UpdateProductsCommand command,
        CancellationToken ct
    )
    {
        var items = command.Request.Items;
        var results = new BatchResultItem[items.Count];
        var hasFailures = false;

        // Step 1: Validate each item individually
        for (var i = 0; i < items.Count; i++)
        {
            var validationResult = await _itemValidator.ValidateAsync(items[i], ct);
            if (!validationResult.IsValid)
            {
                results[i] = new BatchResultItem(
                    i,
                    false,
                    items[i].Id,
                    validationResult.Errors.Select(e => e.ErrorMessage).ToList()
                );
                hasFailures = true;
            }
            else
            {
                results[i] = new BatchResultItem(i, true, items[i].Id, null);
            }
        }

        if (hasFailures)
            return ToBatchResponse(results);

        // Step 2: Load all products in a single query
        var ids = items.Select(item => item.Id).Distinct().ToList();
        var products = await _repository.ListAsync(
            new ProductsByIdsWithLinksSpecification(ids),
            ct
        );
        var productMap = products.ToDictionary(p => p.Id);

        for (var i = 0; i < items.Count; i++)
        {
            if (!productMap.ContainsKey(items[i].Id))
            {
                results[i] = new BatchResultItem(
                    i,
                    false,
                    items[i].Id,
                    [$"Product '{items[i].Id}' not found."]
                );
                hasFailures = true;
            }
        }

        if (hasFailures)
            return ToBatchResponse(results);

        // Step 3: Bulk validate category references
        var allCategoryIds = items
            .Where(item => item.CategoryId.HasValue)
            .Select(item => item.CategoryId!.Value)
            .Distinct()
            .ToList();

        var missingCategoryIds = await ProductValidationHelper.FindMissingCategoryIdsAsync(
            _categoryRepository,
            allCategoryIds,
            ct
        );

        if (missingCategoryIds.Count > 0)
        {
            for (var i = 0; i < items.Count; i++)
            {
                var categoryId = items[i].CategoryId;
                if (categoryId.HasValue && missingCategoryIds.Contains(categoryId.Value))
                {
                    results[i] = new BatchResultItem(
                        i,
                        false,
                        items[i].Id,
                        [$"Category '{categoryId}' not found."]
                    );
                    hasFailures = true;
                }
            }
        }

        // Step 4: Bulk validate product data references
        var allProductDataIds = items
            .Where(item => item.ProductDataIds is { Count: > 0 })
            .SelectMany(item => item.ProductDataIds!)
            .Distinct()
            .ToList();

        var missingProductDataIds = await ProductValidationHelper.FindMissingProductDataIdsAsync(
            _productDataRepository,
            allProductDataIds,
            ct
        );

        if (missingProductDataIds.Count > 0)
        {
            for (var i = 0; i < items.Count; i++)
            {
                if (items[i].ProductDataIds is { Count: > 0 })
                {
                    var missing = items[i]
                        .ProductDataIds!.Where(id => missingProductDataIds.Contains(id))
                        .ToList();

                    if (missing.Count > 0)
                    {
                        results[i] = new BatchResultItem(
                            i,
                            false,
                            items[i].Id,
                            [$"Product data not found: {string.Join(", ", missing)}"]
                        );
                        hasFailures = true;
                    }
                }
            }
        }

        if (hasFailures)
            return ToBatchResponse(results);

        // Step 5: Update all products in a single transaction
        await _unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                for (var i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    var product = productMap[item.Id];

                    product.UpdateDetails(item.Name, item.Description, item.Price, item.CategoryId);

                    if (item.ProductDataIds is not null)
                    {
                        var productDataIds = item.ProductDataIds.Distinct().ToList();
                        var allLinks = await _productDataLinkRepository.ListByProductIdAsync(
                            item.Id,
                            includeDeleted: true,
                            ct
                        );
                        product.SyncProductDataLinks(productDataIds, allLinks);
                    }

                    await _repository.UpdateAsync(product, ct);
                }
            },
            ct
        );

        await _publisher.PublishAsync(new CacheInvalidationNotification(CacheTags.Products), ct);
        return ToBatchResponse(results);
    }

    private static BatchResponse ToBatchResponse(BatchResultItem[] results)
    {
        var successCount = results.Count(r => r.Success);
        return new BatchResponse(results, successCount, results.Length - successCount);
    }
}
