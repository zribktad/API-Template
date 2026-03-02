using APITemplate.Application.DTOs;
using APITemplate.Domain.Entities;

namespace APITemplate.Application.Mappings;

public static class CategoryMappings
{
    public static CategoryResponse ToResponse(this Category category) =>
        new(category.Id, category.Name, category.Description, category.CreatedAt);

    public static ProductCategoryStatsResponse ToResponse(this ProductCategoryStats stats) =>
        new(stats.CategoryId, stats.CategoryName, stats.ProductCount, stats.AveragePrice, stats.TotalReviews);
}
