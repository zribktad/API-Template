using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Features.Category.Mappings;
using APITemplate.Domain.Interfaces;

namespace APITemplate.Application.Features.Category;

/// <summary>Returns aggregated statistics for a category by its identifier, or <see langword="null"/> if not found.</summary>
public sealed record GetCategoryStatsQuery(Guid Id) : IQuery<ProductCategoryStatsResponse?>, IHasId;

/// <summary>Handles <see cref="GetCategoryStatsQuery"/>.</summary>
public sealed class GetCategoryStatsQueryHandler
    : IQueryHandler<GetCategoryStatsQuery, ProductCategoryStatsResponse?>
{
    private readonly ICategoryRepository _repository;

    public GetCategoryStatsQueryHandler(ICategoryRepository repository) => _repository = repository;

    public async Task<ProductCategoryStatsResponse?> HandleAsync(
        GetCategoryStatsQuery request,
        CancellationToken ct
    )
    {
        var stats = await _repository.GetStatsByIdAsync(request.Id, ct);
        return stats?.ToResponse();
    }
}
