using APITemplate.Application.Common.Extensions;
using APITemplate.Domain.Entities;
using APITemplate.Domain.Exceptions;
using APITemplate.Domain.Interfaces;

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
}
