using APITemplate.Application.Common.Search;
using Ardalis.Specification;
using Microsoft.EntityFrameworkCore;
using CategoryEntity = APITemplate.Domain.Entities.Category;

namespace APITemplate.Application.Features.Category.Specifications;

internal static class CategoryFilterCriteria
{
    internal static void ApplyFilter(
        this ISpecificationBuilder<CategoryEntity> query,
        CategoryFilter filter
    )
    {
        if (string.IsNullOrWhiteSpace(filter.Query))
            return;

        query.Where(category =>
            EF.Functions.ToTsVector(
                    SearchDefaults.TextSearchConfiguration,
                    category.Name + " " + (category.Description ?? string.Empty)
                )
                .Matches(
                    EF.Functions.WebSearchToTsQuery(
                        SearchDefaults.TextSearchConfiguration,
                        filter.Query
                    )
                )
        );
    }
}
