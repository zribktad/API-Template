using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Common.CQRS.Rules;
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
        var context = new BatchFailureContext<CreateProductRequest>(items);

        // Step 1: Validate request shape.
        await context.ApplyRulesAsync(
            ct,
            new FluentValidationBatchRule<CreateProductRequest>(_itemValidator)
        );

        // Step 2–3: Reference checks skip only fluent-validation failures so both category and
        // product-data issues can be reported for the same index (merged into one failure row).
        var skipForReferenceChecks = context.FailedIndices.ToHashSet();
        var categoryFailures = await ProductValidationHelper.CheckCategoryReferencesAsync(
            items,
            item => item.CategoryId,
            _categoryRepository,
            skipForReferenceChecks,
            ct
        );
        var productDataFailures = await ProductValidationHelper.CheckProductDataReferencesAsync(
            items,
            item => item.ProductDataIds,
            _productDataRepository,
            skipForReferenceChecks,
            ct
        );
        context.AddFailures(BatchFailureMerge.MergeByIndex(categoryFailures, productDataFailures));

        if (context.HasFailures)
            return context.ToFailureResponse();

        // Step 6: Build entities and persist in a single transaction
        var entities = items
            .Select(item =>
            {
                var productId = Guid.NewGuid();
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
