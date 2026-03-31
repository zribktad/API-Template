using System.Linq.Expressions;
using ProductCatalog.Application.Features.Category.DTOs;
using CategoryEntity = ProductCatalog.Domain.Entities.Category;
using ProductCategoryStatsEntity = ProductCatalog.Domain.Entities.ProductCategoryStats;

namespace ProductCatalog.Application.Features.Category.Mappings;

/// <summary>
/// Provides mapping utilities between category domain entities and their response DTOs.
/// </summary>
public static class CategoryMappings
{
    /// <summary>
    /// EF Core-compatible expression that projects a <see cref="CategoryEntity"/> to a <see cref="CategoryResponse"/>.
    /// </summary>
    public static readonly Expression<Func<CategoryEntity, CategoryResponse>> Projection =
        category => new CategoryResponse(
            category.Id,
            category.Name,
            category.Description,
            category.Audit.CreatedAtUtc
        );

    private static readonly Func<CategoryEntity, CategoryResponse> CompiledProjection =
        Projection.Compile();

    /// <summary>Maps a <see cref="CategoryEntity"/> to a <see cref="CategoryResponse"/> using the compiled projection.</summary>
    public static CategoryResponse ToResponse(this CategoryEntity category) =>
        CompiledProjection(category);

    /// <summary>Maps a <see cref="ProductCategoryStatsEntity"/> to a <see cref="ProductCategoryStatsResponse"/>.</summary>
    public static ProductCategoryStatsResponse ToResponse(this ProductCategoryStatsEntity stats) =>
        new(
            stats.CategoryId,
            stats.CategoryName,
            stats.ProductCount,
            stats.AveragePrice,
            stats.TotalReviews
        );
}
