using APITemplate.Application.Common.Extensions;
using APITemplate.Domain.Exceptions;

namespace APITemplate.Application.Features.Product;

/// <summary>Shared validation methods for product commands.</summary>
internal static class ProductValidationHelper
{
    internal static async Task ValidateCategoryExistsAsync(
        ICategoryRepository categoryRepository,
        Guid? categoryId,
        CancellationToken ct
    )
    {
        if (!categoryId.HasValue)
            return;

        await categoryRepository.GetByIdOrThrowAsync(
            categoryId.Value,
            ErrorCatalog.Categories.NotFound,
            ct
        );
    }

    internal static async Task<IReadOnlyCollection<Guid>> ValidateAndNormalizeProductDataIdsAsync(
        IProductDataRepository productDataRepository,
        IReadOnlyCollection<Guid> productDataIds,
        CancellationToken ct
    )
    {
        var normalizedIds = productDataIds.Distinct().ToArray();

        if (normalizedIds.Length == 0)
            return normalizedIds;

        var existingIds = (await productDataRepository.GetByIdsAsync(normalizedIds, ct))
            .Select(productData => productData.Id)
            .ToHashSet();

        var missingIds = normalizedIds.Where(id => !existingIds.Contains(id)).ToArray();

        if (missingIds.Length > 0)
        {
            throw new NotFoundException(
                nameof(ProductData),
                string.Join(", ", missingIds),
                ErrorCatalog.Products.ProductDataNotFound
            );
        }

        return normalizedIds;
    }

    /// <summary>
    /// Checks that all referenced category IDs exist and returns per-item failures for items
    /// that reference a missing category. Items in <paramref name="failedIndices"/> are skipped.
    /// </summary>
    internal static async Task<List<BatchResultItem>> CheckCategoryReferencesAsync<T>(
        IReadOnlyList<T> items,
        Func<T, Guid?> categoryIdSelector,
        ICategoryRepository categoryRepository,
        IReadOnlySet<int> failedIndices,
        CancellationToken ct
    )
    {
        var allCategoryIds = items
            .Where(item => categoryIdSelector(item).HasValue)
            .Select(item => categoryIdSelector(item)!.Value)
            .ToHashSet();

        if (allCategoryIds.Count == 0)
            return [];

        var existing = await categoryRepository.ListAsync(
            new Category.Specifications.CategoriesByIdsSpecification(allCategoryIds),
            ct
        );
        allCategoryIds.ExceptWith(existing.Select(c => c.Id));

        if (allCategoryIds.Count == 0)
            return [];

        var failures = new List<BatchResultItem>();

        for (var i = 0; i < items.Count; i++)
        {
            if (failedIndices.Contains(i))
                continue;

            var categoryId = categoryIdSelector(items[i]);
            if (categoryId.HasValue && allCategoryIds.Contains(categoryId.Value))
            {
                Guid? failureId = items[i] is IHasId hasId ? hasId.Id : null;
                failures.Add(
                    new BatchResultItem(
                        i,
                        failureId,
                        [string.Format(ErrorCatalog.Categories.NotFoundMessage, categoryId)]
                    )
                );
            }
        }

        return failures;
    }

    /// <summary>
    /// Checks that all referenced product-data IDs exist and returns per-item failures for items
    /// that reference missing product data. Items in <paramref name="failedIndices"/> are skipped.
    /// </summary>
    internal static async Task<List<BatchResultItem>> CheckProductDataReferencesAsync<T>(
        IReadOnlyList<T> items,
        Func<T, IReadOnlyCollection<Guid>?> productDataIdsSelector,
        IProductDataRepository productDataRepository,
        IReadOnlySet<int> failedIndices,
        CancellationToken ct
    )
    {
        var allProductDataIds = items
            .Where(item => productDataIdsSelector(item) is { Count: > 0 })
            .SelectMany(item => productDataIdsSelector(item)!)
            .Distinct()
            .ToArray();

        if (allProductDataIds.Length == 0)
            return [];

        var existingIds = (await productDataRepository.GetByIdsAsync(allProductDataIds, ct))
            .Select(pd => pd.Id)
            .ToHashSet();

        var missingIds = allProductDataIds.Where(id => !existingIds.Contains(id)).ToHashSet();

        if (missingIds.Count == 0)
            return [];

        var failures = new List<BatchResultItem>();

        for (var i = 0; i < items.Count; i++)
        {
            if (failedIndices.Contains(i))
                continue;

            var pdIds = productDataIdsSelector(items[i]);
            if (pdIds is not { Count: > 0 })
                continue;

            var missing = pdIds.Where(id => missingIds.Contains(id)).ToList();
            if (missing.Count > 0)
            {
                Guid? failureId = items[i] is IHasId hasId ? hasId.Id : null;
                failures.Add(
                    new BatchResultItem(
                        i,
                        failureId,
                        [$"Product data not found: {string.Join(", ", missing)}"]
                    )
                );
            }
        }

        return failures;
    }
}
