namespace APITemplate.Api.GraphQL.Queries;

public class ProductQueries
{
    [UsePaging(MaxPageSize = 100, DefaultPageSize = 20)]
    public async Task<IEnumerable<ProductResponse>> GetProducts(
        [Service] IProductQueryService queryService,
        CancellationToken ct)
        => await queryService.GetAllAsync(ct);

    public async Task<ProductResponse?> GetProductById(
        Guid id,
        [Service] IProductQueryService queryService,
        CancellationToken ct)
        => await queryService.GetByIdAsync(id, ct);
}
