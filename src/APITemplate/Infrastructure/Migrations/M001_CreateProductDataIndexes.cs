using APITemplate.Domain.Entities;
using MongoDB.Driver;

namespace APITemplate.Infrastructure.Migrations;

/// <summary>
/// Creates indexes on the product_data collection:
///   - idx_type    : ascending on _t (discriminator) — speeds up ?type=image|video filter
///   - idx_created : descending on CreatedAt — speeds up time-based ordering
/// </summary>
public sealed class M001_CreateProductDataIndexes : IMigration
{
    public int Version => 1;
    public string Description => "Create indexes on product_data collection";

    public async Task UpAsync(IMongoDatabase database, CancellationToken ct = default)
    {
        var collection = database.GetCollection<ProductData>("product_data");

        var indexes = new[]
        {
            new CreateIndexModel<ProductData>(
                Builders<ProductData>.IndexKeys.Ascending("_t"),
                new CreateIndexOptions { Name = "idx_type" }),

            new CreateIndexModel<ProductData>(
                Builders<ProductData>.IndexKeys.Descending(x => x.CreatedAt),
                new CreateIndexOptions { Name = "idx_created" })
        };

        await collection.Indexes.CreateManyAsync(indexes, ct);
    }
}
