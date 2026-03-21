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
    /// Validates that all non-null category IDs exist in the database using a single query.
    /// Returns the set of missing IDs instead of throwing, so callers can map errors to per-item results.
    /// </summary>
    internal static async Task<HashSet<Guid>> FindMissingCategoryIdsAsync(
        ICategoryRepository categoryRepository,
        IReadOnlyCollection<Guid> categoryIds,
        CancellationToken ct
    )
    {
        if (categoryIds.Count == 0)
            return [];

        var distinctIds = categoryIds.Distinct().ToHashSet();
        var existing = await categoryRepository.ListAsync(
            new Category.Specifications.CategoriesByIdsSpecification(distinctIds),
            ct
        );
        var existingIds = existing.Select(c => c.Id).ToHashSet();

        distinctIds.ExceptWith(existingIds);
        return distinctIds;
    }

    /// <summary>
    /// Validates that all product-data IDs exist in the database using a single query.
    /// Returns the set of missing IDs instead of throwing, so callers can map errors to per-item results.
    /// </summary>
    internal static async Task<HashSet<Guid>> FindMissingProductDataIdsAsync(
        IProductDataRepository productDataRepository,
        IReadOnlyCollection<Guid> productDataIds,
        CancellationToken ct
    )
    {
        var distinctIds = productDataIds.Distinct().ToArray();

        if (distinctIds.Length == 0)
            return [];

        var existingIds = (await productDataRepository.GetByIdsAsync(distinctIds, ct))
            .Select(pd => pd.Id)
            .ToHashSet();

        return distinctIds.Where(id => !existingIds.Contains(id)).ToHashSet();
    }
}
