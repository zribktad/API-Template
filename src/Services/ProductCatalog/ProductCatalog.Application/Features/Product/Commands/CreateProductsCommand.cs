using Contracts.IntegrationEvents.ProductCatalog;
using ErrorOr;
using FluentValidation;
using ProductCatalog.Application.Features.Product.DTOs;
using ProductCatalog.Application.Features.Product.Repositories;
using ProductCatalog.Domain.Entities;
using ProductCatalog.Domain.Interfaces;
using SharedKernel.Application.Batch;
using SharedKernel.Application.Batch.Rules;
using SharedKernel.Application.DTOs;
using SharedKernel.Domain.Interfaces;
using Wolverine;
using ProductEntity = ProductCatalog.Domain.Entities.Product;

namespace ProductCatalog.Application.Features.Product.Commands;

/// <summary>Creates multiple products in a single batch operation.</summary>
public sealed record CreateProductsCommand(CreateProductsRequest Request);

/// <summary>Handles <see cref="CreateProductsCommand"/> by validating all items, bulk-validating references, and persisting in a single transaction.</summary>
public sealed class CreateProductsCommandHandler
{
    public static async Task<ErrorOr<BatchResponse>> HandleAsync(
        CreateProductsCommand command,
        IProductRepository repository,
        ICategoryRepository categoryRepository,
        IProductDataRepository productDataRepository,
        IUnitOfWork unitOfWork,
        IMessageBus bus,
        IValidator<CreateProductRequest> itemValidator,
        TimeProvider timeProvider,
        CancellationToken ct
    )
    {
        IReadOnlyList<CreateProductRequest> items = command.Request.Items;
        BatchFailureContext<CreateProductRequest> context = new(items);

        await context.ApplyRulesAsync(
            ct,
            new FluentValidationBatchRule<CreateProductRequest>(itemValidator)
        );

        // Reference checks skip only fluent-validation failures so both category and
        // product-data issues can be reported for the same index (merged into one failure row).
        context.AddFailures(
            await ProductValidationHelper.CheckProductReferencesAsync(
                items,
                categoryRepository,
                productDataRepository,
                context.FailedIndices,
                ct
            )
        );

        if (context.HasFailures)
            return context.ToFailureResponse();

        // Build entities and persist in a single transaction
        List<ProductEntity> entities = items
            .Select(item =>
            {
                Guid productId = Guid.NewGuid();
                return new ProductEntity
                {
                    Id = productId,
                    Name = item.Name,
                    Description = item.Description,
                    Price = item.Price,
                    CategoryId = item.CategoryId,
                    ProductDataLinks = (item.ProductDataIds ?? [])
                        .Distinct()
                        .Select(pdId => ProductDataLink.Create(productId, pdId))
                        .ToList(),
                };
            })
            .ToList();

        await unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                await repository.AddRangeAsync(entities, ct);
            },
            ct
        );

        // Publish integration events for each created product
        DateTime occurredAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        foreach (ProductEntity entity in entities)
        {
            await bus.PublishAsync(
                new ProductCreatedIntegrationEvent(
                    entity.Id,
                    entity.TenantId,
                    entity.Name,
                    occurredAtUtc
                )
            );
        }

        return new BatchResponse([], items.Count, 0);
    }
}
