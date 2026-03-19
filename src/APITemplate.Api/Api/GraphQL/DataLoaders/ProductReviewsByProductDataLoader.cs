using MediatR;

namespace APITemplate.Api.GraphQL.DataLoaders;

/// <summary>
/// Hot Chocolate batch data loader that resolves all reviews for a set of product IDs in a
/// single MediatR query, preventing the N+1 problem when the GraphQL schema resolves reviews
/// as a field on <c>ProductType</c>.
/// </summary>
public sealed class ProductReviewsByProductDataLoader
    : BatchDataLoader<Guid, ProductReviewResponse[]>
{
    private readonly ISender _sender;

    public ProductReviewsByProductDataLoader(
        ISender sender,
        IBatchScheduler batchScheduler,
        DataLoaderOptions options = default!
    )
        : base(batchScheduler, options)
    {
        _sender = sender;
    }

    /// <summary>
    /// Fetches all reviews for the supplied <paramref name="productIds"/> in one round-trip
    /// and returns a dictionary keyed by product ID.
    /// </summary>
    protected override async Task<
        IReadOnlyDictionary<Guid, ProductReviewResponse[]>
    > LoadBatchAsync(IReadOnlyList<Guid> productIds, CancellationToken ct)
    {
        return await _sender.Send(new GetProductReviewsByProductIdsQuery(productIds), ct);
    }
}
