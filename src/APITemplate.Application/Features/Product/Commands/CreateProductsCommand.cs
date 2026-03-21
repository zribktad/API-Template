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

        // Step 1: Validate each item (field-level rules — name, price, etc.)
        var failures = await BatchHelper.ValidateAsync(_itemValidator, items, _ => null, ct);

        if (failures.Count > 0)
            return new BatchResponse(failures, items.Count - failures.Count, failures.Count);

        // Step 2: Verify all referenced categories exist
        var allCategoryIds = items
            .Where(item => item.CategoryId.HasValue)
            .Select(item => item.CategoryId!.Value)
            .ToHashSet();

        var missingCategoryIds = await ProductValidationHelper.FindMissingCategoryIdsAsync(
            _categoryRepository,
            allCategoryIds,
            ct
        );

        var failedIndices = new HashSet<int>();

        if (missingCategoryIds.Count > 0)
        {
            for (var i = 0; i < items.Count; i++)
            {
                var categoryId = items[i].CategoryId;
                if (categoryId.HasValue && missingCategoryIds.Contains(categoryId.Value))
                {
                    failures.Add(
                        new BatchResultItem(
                            i,
                            null,
                            [string.Format(ErrorCatalog.Categories.NotFoundMessage, categoryId)]
                        )
                    );
                    failedIndices.Add(i);
                }
            }
        }

        // Step 3: Verify all referenced product data entries exist
        var allProductDataIds = items
            .Where(item => item.ProductDataIds is { Count: > 0 })
            .SelectMany(item => item.ProductDataIds!)
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
                if (!failedIndices.Contains(i) && items[i].ProductDataIds is { Count: > 0 })
                {
                    var missing = items[i]
                        .ProductDataIds!.Where(id => missingProductDataIds.Contains(id))
                        .ToList();

                    if (missing.Count > 0)
                    {
                        failures.Add(
                            new BatchResultItem(
                                i,
                                null,
                                [$"Product data not found: {string.Join(", ", missing)}"]
                            )
                        );
                    }
                }
            }
        }

        if (failures.Count > 0)
            return new BatchResponse(failures, items.Count - failures.Count, failures.Count);

        // Step 4: Build entities and persist in a single transaction
        var entities = items
            .Select(item =>
            {
                var productId = item.Id ?? Guid.NewGuid();
                var productDataIds = (item.ProductDataIds ?? []).Distinct().ToList();

                return new ProductEntity
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
            })
            .ToList();

        await _unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                await _repository.AddRangeAsync(entities, ct);
            },
            ct
        );

        await _publisher.PublishAsync(new CacheInvalidationNotification(CacheTags.Products), ct);
        return new BatchResponse([], items.Count, 0);
    }
}
