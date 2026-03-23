using APITemplate.Application.Common.Batch;
using APITemplate.Application.Common.Batch.Rules;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Features.Product.Specifications;
using APITemplate.Domain.Entities;
using FluentValidation;
using Wolverine;

namespace APITemplate.Application.Features.Product;

/// <summary>Updates multiple products in a single batch operation.</summary>
public sealed record UpdateProductsCommand(UpdateProductsRequest Request);

/// <summary>Handles <see cref="UpdateProductsCommand"/> by validating all items, loading products in bulk, and updating in a single transaction.</summary>
public sealed class UpdateProductsCommandHandler
{
    public static async Task<BatchResponse> HandleAsync(
        UpdateProductsCommand command,
        IProductRepository repository,
        ICategoryRepository categoryRepository,
        IProductDataRepository productDataRepository,
        IProductDataLinkRepository productDataLinkRepository,
        IUnitOfWork unitOfWork,
        IMessageBus bus,
        IValidator<UpdateProductItem> itemValidator,
        CancellationToken ct
    )
    {
        var items = command.Request.Items;
        var context = new BatchFailureContext<UpdateProductItem>(items);

        // Step 1: Validate each item (field-level rules — name, price, etc.)
        await context.ApplyRulesAsync(
            ct,
            new FluentValidationBatchRule<UpdateProductItem>(itemValidator)
        );

        // Step 2: Load all target products and mark missing ones as failed
        var requestedIds = items.Select(item => item.Id).ToHashSet();
        var productMap = (
            await repository.ListAsync(new ProductsByIdsWithLinksSpecification(requestedIds), ct)
        ).ToDictionary(p => p.Id);

        await context.ApplyRulesAsync(
            ct,
            new MarkMissingByIdBatchRule<UpdateProductItem>(
                productMap.Keys.ToHashSet(),
                ErrorCatalog.Products.NotFoundMessage
            )
        );

        // Step 3–4: Reference checks skip only earlier failures (validation + missing entity) so
        // category and product-data issues on the same row are merged into one failure.
        var skipForReferenceChecks = context.FailedIndices.ToHashSet();
        var categoryFailures = await ProductValidationHelper.CheckCategoryReferencesAsync(
            items,
            item => item.CategoryId,
            categoryRepository,
            skipForReferenceChecks,
            ct
        );
        var productDataFailures = await ProductValidationHelper.CheckProductDataReferencesAsync(
            items,
            item => item.ProductDataIds,
            productDataRepository,
            skipForReferenceChecks,
            ct
        );
        context.AddFailures(BatchFailureMerge.MergeByIndex(categoryFailures, productDataFailures));

        if (context.HasFailures)
            return context.ToFailureResponse();

        // Step 5: Apply changes and sync product-data links in a single transaction
        await unitOfWork.ExecuteInTransactionAsync(
            async () =>
            {
                for (var i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    var productId = item.Id;
                    var product = productMap[productId];

                    product.UpdateDetails(item.Name, item.Description, item.Price, item.CategoryId);

                    if (item.ProductDataIds is not null)
                    {
                        var targetIds = item.ProductDataIds.ToHashSet();
                        var existingById = product.ProductDataLinks.ToDictionary(link =>
                            link.ProductDataId
                        );
                        product.SyncProductDataLinks(targetIds, existingById);
                    }

                    await repository.UpdateAsync(product, ct);
                }
            },
            ct
        );

        await bus.PublishAsync(new CacheInvalidationNotification(CacheTags.Products));
        return new BatchResponse([], items.Count, 0);
    }
}
