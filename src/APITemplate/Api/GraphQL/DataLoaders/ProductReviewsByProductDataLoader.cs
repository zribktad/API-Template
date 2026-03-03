namespace APITemplate.Api.GraphQL.DataLoaders;

// WHEN TO USE THIS DATALOADER?
//
// This DataLoader is only needed when ProductType.Reviews has a custom resolver —
// for example when data comes from a different source (REST API, cache, another DB).
//
// Without DataLoader, a naive resolver causes the N+1 problem:
// For 100 products → 101 SQL queries (1 for products + 1 per product for reviews).
//
// --- STEP 1: Create a resolver class (instead of inline ResolveWith lambda) ---
//
// internal sealed class ProductTypeResolvers
// {
//     // Called once per product — without DataLoader this triggers N queries
//     public Task<ProductReview[]> GetReviews(
//         [Parent] Product product,
//         ProductReviewsByProductDataLoader loader)
//         => loader.LoadAsync(product.Id);
//         // DataLoader batches all product IDs from the request and calls
//         // LoadBatchAsync once with the full list → 1 SQL query instead of N
// }
//
// --- STEP 2: Wire the resolver in ProductType.cs ---
//
// In ProductType.Configure(), replace:
//
//   descriptor.Field(p => p.Reviews)
//       .Description("The reviews associated with this product.");
//
// With:
//
//   descriptor.Field(p => p.Reviews)
//       .ResolveWith<ProductTypeResolvers>(r => r.GetReviews(default!, default!))
//       .Description("The reviews associated with this product.");
//
// --- STEP 3: Register the DataLoader in ServiceCollectionExtensions.cs ---
//
// In AddGraphQLConfiguration(), add:
//
//   .AddDataLoader<ProductReviewsByProductDataLoader>()
//
// NOTE: In this project the DataLoader is NOT needed because resolvers return
// IQueryable<Product> with [UseProjection] — EF Core automatically generates
// a JOIN and selects only the requested fields in a single SQL query.
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
        var reviews = await _queryService.GetAllAsync(ct);

        // Group in memory — ToLookup is a single pass and returns [] for missing keys
        var lookup = reviews.ToLookup(r => r.ProductId);

        // Every input key must have an entry in the result (even if reviews is empty)
        return productIds
            .Distinct()
            .ToDictionary(id => id, id => lookup[id].ToArray());
    }
}
