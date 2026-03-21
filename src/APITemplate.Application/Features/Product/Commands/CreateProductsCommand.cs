using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Features.Product.Specifications;
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
        var failures = await BatchFailureCollectorHelper.ValidateAsync(
            _itemValidator,
            items,
            _ => null,
            ct
        );
        var failedIndices = failures.Select(f => f.Index).ToHashSet();

        // Step 2: Reject duplicate client-supplied IDs inside the request
        var duplicateIdFailures = BatchFailureCollectorHelper.MarkDuplicateOptionalIds(
            items,
            item => item.Id,
            ErrorCatalog.Products.DuplicateIdMessage,
            failedIndices
        );
        failures.AddRange(duplicateIdFailures);
        failedIndices.UnionWith(duplicateIdFailures.Select(f => f.Index));

        // Step 3: Reject client-supplied IDs that already exist (including soft-deleted rows)
        var explicitIds = items
            .Select((item, index) => new { Item = item, Index = index })
            .Where(x => !failedIndices.Contains(x.Index) && x.Item.Id.HasValue)
            .Select(x => x.Item.Id!.Value)
            .ToHashSet();

        if (explicitIds.Count > 0)
        {
            var existingIds = (
                await _repository.ListAsync(
                    new ProductsByIdsWithLinksSpecification(explicitIds, includeDeleted: true),
                    ct
                )
            )
                .Select(product => product.Id)
                .ToHashSet();

            var existingIdFailures = BatchFailureCollectorHelper.MarkExistingOptionalIds(
                items,
                item => item.Id,
                existingIds,
                ErrorCatalog.Products.AlreadyExistsMessage,
                failedIndices
            );
            failures.AddRange(existingIdFailures);
            failedIndices.UnionWith(existingIdFailures.Select(f => f.Index));
        }

        // Step 4: Verify all referenced categories exist
        failures.AddRange(
            await ProductValidationHelper.CheckCategoryReferencesAsync(
                items,
                item => item.CategoryId,
                _ => null,
                _categoryRepository,
                failedIndices,
                ct
            )
        );

        // Step 5: Verify all referenced product data entries exist
        failures.AddRange(
            await ProductValidationHelper.CheckProductDataReferencesAsync(
                items,
                item => item.ProductDataIds,
                _ => null,
                _productDataRepository,
                failedIndices,
                ct
            )
        );

        if (failures.Count > 0)
            return new BatchResponse(failures, 0, failures.Count);

        // Step 6: Build entities and persist in a single transaction
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
