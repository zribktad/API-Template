using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Features.Product.Repositories;
using APITemplate.Application.Features.Product.Specifications;

namespace APITemplate.Application.Features.Product;

/// <summary>Retrieves a single product by its unique identifier.</summary>
public sealed record GetProductByIdQuery(Guid Id) : IQuery<ProductResponse?>, IHasId;

/// <summary>Handles <see cref="GetProductByIdQuery"/> by fetching from the product repository.</summary>
public sealed class GetProductByIdQueryHandler
    : IQueryHandler<GetProductByIdQuery, ProductResponse?>
{
    private readonly IProductRepository _repository;

    public GetProductByIdQueryHandler(IProductRepository repository) => _repository = repository;

    public async Task<ProductResponse?> HandleAsync(
        GetProductByIdQuery request,
        CancellationToken ct
    ) => await _repository.FirstOrDefaultAsync(new ProductByIdSpecification(request.Id), ct);
}
