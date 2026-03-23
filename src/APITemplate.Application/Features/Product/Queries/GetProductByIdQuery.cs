using APITemplate.Application.Features.Product.Repositories;
using APITemplate.Application.Features.Product.Specifications;

namespace APITemplate.Application.Features.Product;

/// <summary>Retrieves a single product by its unique identifier.</summary>
public sealed record GetProductByIdQuery(Guid Id) : IHasId;

/// <summary>Handles <see cref="GetProductByIdQuery"/> by fetching from the product repository.</summary>
public sealed class GetProductByIdQueryHandler
{
    public static async Task<ProductResponse?> HandleAsync(
        GetProductByIdQuery request,
        IProductRepository repository,
        CancellationToken ct
    ) => await repository.FirstOrDefaultAsync(new ProductByIdSpecification(request.Id), ct);
}
