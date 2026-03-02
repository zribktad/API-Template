using APITemplate.Domain.Entities;
using APITemplate.Domain.Interfaces;
using APITemplate.Infrastructure.Persistence;
using MongoDB.Bson;
using MongoDB.Driver;

namespace APITemplate.Infrastructure.Repositories;

public sealed class ProductDataRepository : IProductDataRepository
{
    private readonly IMongoCollection<ProductData> _collection;

    public ProductDataRepository(MongoDbContext context)
    {
        _collection = context.ProductData;
    }

    public async Task<ProductData?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        if (!ObjectId.TryParse(id, out _))
            return null;

        return await _collection
            .Find(x => x.Id == id)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<ProductData>> GetAllAsync(string? type = null, CancellationToken ct = default)
    {
        var filter = type is null
            ? Builders<ProductData>.Filter.Empty
            : Builders<ProductData>.Filter.Eq("_t", type);

        return await _collection.Find(filter).ToListAsync(ct);
    }

    public async Task<ProductData> CreateAsync(ProductData productData, CancellationToken ct = default)
    {
        await _collection.InsertOneAsync(productData, cancellationToken: ct);
        return productData;
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        await _collection.DeleteOneAsync(x => x.Id == id, ct);
    }
}
