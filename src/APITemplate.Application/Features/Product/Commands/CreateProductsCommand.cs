using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Common.Events;
using APITemplate.Domain.Entities;
using FluentValidation;
using ProductEntity = APITemplate.Domain.Entities.Product;

namespace APITemplate.Application.Features.Product;

/// <summary>Creates multiple products in a single batch operation.</summary>
public sealed record CreateProductsCommand(CreateProductsRequest Request) : ICommand<BatchResponse>;

/// <summary>Handles <see cref="CreateProductsCommand"/> by validating all items, bulk-validating references, and persisting in a single transaction.</summary>
public sealed class CreateProductsCommandHandler
    : ICommandHandler<CreateProductsCommand, BatchResponse>
{
    private readonly IProductRepository _repository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IProductDataRepository _productDataRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _publisher;
    private readonly IValidator<CreateProductRequest> _itemValidator;

    public CreateProductsCommandHandler(
        IProductRepository repository,
        ICategoryRepository categoryRepository,
        IProductDataRepository productDataRepository,
        IUnitOfWork unitOfWork,
        IEventPublisher publisher,
        IValidator<CreateProductRequest> itemValidator
    )
    {
        _repository = repository;
        _categoryRepository = categoryRepository;
        _productDataRepository = productDataRepository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
        _itemValidator = itemValidator;
    }

    public async Task<BatchResponse> HandleAsync(
        CreateProductsCommand command,
        CancellationToken ct
    )
    {
        var items = command.Request.Items;
        var results = new BatchResultItem[items.Count];
        var failureCount = await BatchHelper.ValidateAsync(
            _itemValidator,
            items,
            results,
            _ => null,
            ct
        );

        if (failureCount > 0)
            return new BatchResponse(results, results.Length - failureCount, failureCount);

        // Step 2: Bulk validate category references
        var allCategoryIds = items
            .Where(item => item.CategoryId.HasValue)
            .Select(item => item.CategoryId!.Value)
            .Distinct()
            .ToHashSet();

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
                        null,
                        [string.Format(ErrorCatalog.Categories.NotFoundMessage, categoryId)]
                    );
                    failureCount++;
                }
            }
        }

        // Step 3: Bulk validate product data references
        var allProductDataIds = items
            .Where(item => item.ProductDataIds is { Count: > 0 })
            .SelectMany(item => item.ProductDataIds!)
            .Distinct()
            .ToHashSet();

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
                            null,
                            [$"Product data not found: {string.Join(", ", missing)}"]
                        );
                        failureCount++;
                    }
                }
            }
        }

        if (failureCount > 0)
            return new BatchResponse(results, results.Length - failureCount, failureCount);

        // Step 4: Create all entities in a single transaction
        var entities = new List<ProductEntity>(items.Count);

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var productId = Guid.NewGuid();
            var productDataIds = (item.ProductDataIds ?? []).Distinct().ToList();

            var entity = new ProductEntity
            {
                Id = productId,
                Name = item.Name,
                Description = item.Description,
                Price = item.Price,
                CategoryId = item.CategoryId,
                ProductDataLinks = productDataIds
                    .Select(pdId => ProductDataLink.Create(productId, pdId))
                    .ToList(),
            };

            entities.Add(entity);
            results[i] = new BatchResultItem(i, true, productId, null);
        }

        await _unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                await _repository.AddRangeAsync(entities, ct);
            },
            ct
        );

        await _publisher.PublishAsync(new CacheInvalidationNotification(CacheTags.Products), ct);
        return new BatchResponse(results, results.Length, 0);
    }
}
