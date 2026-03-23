using APITemplate.Application.Features.Category.Mappings;
using APITemplate.Domain.Interfaces;

namespace APITemplate.Application.Features.Category;

/// <summary>Returns aggregated statistics for a category by its identifier, or <see langword="null"/> if not found.</summary>
public sealed record GetCategoryStatsQuery(Guid Id) : IHasId;

/// <summary>Handles <see cref="GetCategoryStatsQuery"/>.</summary>
public sealed class GetCategoryStatsQueryHandler
{
    public static async Task<ProductCategoryStatsResponse?> HandleAsync(
        GetCategoryStatsQuery request,
        ICategoryRepository repository,
        CancellationToken ct
    )
    {
        var stats = await repository.GetStatsByIdAsync(request.Id, ct);
        return stats?.ToResponse();
    }
}
