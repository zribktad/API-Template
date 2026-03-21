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

        // Step 1: Validate each item (field-level rules — name, price, etc.)
        var failures = await BatchHelper.ValidateAsync(_itemValidator, items, i => items[i].Id, ct);
        var failedIndices = failures.Select(f => f.Index).ToHashSet();

        // Step 2: Load all target products and mark missing ones as failed
        var productMap = (
            await _repository.ListAsync(
                new ProductsByIdsWithLinksSpecification(items.Select(item => item.Id).ToHashSet()),
                ct
            )
        ).ToDictionary(p => p.Id);

        var missingFailures = BatchHelper.MarkMissing(
            items,
            item => item.Id,
            productMap.ContainsKey,
            ErrorCatalog.Products.NotFoundMessage,
            failedIndices
        );
        failures.AddRange(missingFailures);
        failedIndices.UnionWith(missingFailures.Select(f => f.Index));

        // Step 3: Verify all referenced categories exist
        failures.AddRange(
            await ProductValidationHelper.CheckCategoryReferencesAsync(
                items,
                item => item.CategoryId,
                i => items[i].Id,
                _categoryRepository,
                failedIndices,
                ct
            )
        );

        // Step 4: Verify all referenced product data entries exist
        failures.AddRange(
            await ProductValidationHelper.CheckProductDataReferencesAsync(
                items,
                item => item.ProductDataIds,
                i => items[i].Id,
                _productDataRepository,
                failedIndices,
                ct
            )
        );

        if (failures.Count > 0)
            return new BatchResponse(failures, items.Count - failures.Count, failures.Count);

        // Step 5: Apply changes and sync product-data links in a single transaction
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
        return new BatchResponse([], items.Count, 0);
    }
}
