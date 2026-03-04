namespace APITemplate.Api.GraphQL.DataLoaders;
public sealed class ProductReviewsByProductDataLoader : BatchDataLoader<Guid, ProductReviewResponse[]>
{
    private readonly IProductReviewQueryService _queryService;

    public ProductReviewsByProductDataLoader(
        IProductReviewQueryService queryService,
        IBatchScheduler batchScheduler,
        DataLoaderOptions options = default!)
        : base(batchScheduler, options)
    {
        _queryService = queryService;
    }

    protected override async Task<IReadOnlyDictionary<Guid, ProductReviewResponse[]>> LoadBatchAsync(
        IReadOnlyList<Guid> productIds,
        CancellationToken ct)
    {
        return await _queryService.GetByProductIdsAsync(productIds, ct);
    }
}
