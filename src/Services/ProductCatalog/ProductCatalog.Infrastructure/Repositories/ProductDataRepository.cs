using MongoDB.Driver;
using ProductCatalog.Domain.Entities.ProductData;
using ProductCatalog.Domain.Interfaces;
using ProductCatalog.Infrastructure.Persistence;
using SharedKernel.Application.Context;

namespace ProductCatalog.Infrastructure.Repositories;

/// <summary>
/// MongoDB repository for <see cref="ProductData"/> documents, applying tenant and soft-delete
/// isolation at the query level since MongoDB has no EF Core global filter equivalent.
/// </summary>
public sealed class ProductDataRepository : IProductDataRepository
{
    private readonly IMongoCollection<ProductData> _collection;
    private readonly ITenantProvider _tenantProvider;

    public ProductDataRepository(MongoDbContext context, ITenantProvider tenantProvider)
    {
        _collection = context.ProductData;
        _tenantProvider = tenantProvider;
    }

    public async Task<ProductData?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _collection
            .Find(x => x.Id == id && x.TenantId == _tenantProvider.TenantId && !x.IsDeleted)
            .FirstOrDefaultAsync(ct);

    public async Task<List<ProductData>> GetByIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken ct = default
    )
    {
        Guid[] idArray = ids.Distinct().ToArray();

        if (idArray.Length == 0)
            return [];

        return await _collection
            .Find(
                Builders<ProductData>.Filter.And(
                    Builders<ProductData>.Filter.In(x => x.Id, idArray),
                    Builders<ProductData>.Filter.Eq(x => x.TenantId, _tenantProvider.TenantId),
                    Builders<ProductData>.Filter.Eq(x => x.IsDeleted, false)
                )
            )
            .ToListAsync(ct);
    }

    public async Task<List<ProductData>> GetAllAsync(
        string? type = null,
        CancellationToken ct = default
    )
    {
        FilterDefinition<ProductData> filter = type is null
            ? Builders<ProductData>.Filter.And(
                Builders<ProductData>.Filter.Eq(x => x.TenantId, _tenantProvider.TenantId),
                Builders<ProductData>.Filter.Eq(x => x.IsDeleted, false)
            )
            : Builders<ProductData>.Filter.And(
                Builders<ProductData>.Filter.Eq(x => x.TenantId, _tenantProvider.TenantId),
                Builders<ProductData>.Filter.Eq("_t", type),
                Builders<ProductData>.Filter.Eq(x => x.IsDeleted, false)
            );

        return await _collection.Find(filter).ToListAsync(ct);
    }

    public async Task<ProductData> CreateAsync(
        ProductData productData,
        CancellationToken ct = default
    )
    {
        await _collection.InsertOneAsync(productData, cancellationToken: ct);
        return productData;
    }

    public async Task SoftDeleteAsync(
        Guid id,
        Guid actorId,
        DateTime deletedAtUtc,
        CancellationToken ct = default
    )
    {
        UpdateDefinition<ProductData> update = Builders<ProductData>
            .Update.Set(x => x.IsDeleted, true)
            .Set(x => x.DeletedAtUtc, deletedAtUtc)
            .Set(x => x.DeletedBy, actorId);

        await _collection.UpdateOneAsync(
            x => x.Id == id && x.TenantId == _tenantProvider.TenantId && !x.IsDeleted,
            update,
            cancellationToken: ct
        );
    }

    public async Task<long> SoftDeleteByTenantAsync(
        Guid tenantId,
        Guid actorId,
        DateTime deletedAtUtc,
        CancellationToken ct = default
    )
    {
        FilterDefinition<ProductData> filter = Builders<ProductData>.Filter.And(
            Builders<ProductData>.Filter.Eq(x => x.TenantId, tenantId),
            Builders<ProductData>.Filter.Eq(x => x.IsDeleted, false)
        );

        UpdateDefinition<ProductData> update = Builders<ProductData>
            .Update.Set(x => x.IsDeleted, true)
            .Set(x => x.DeletedAtUtc, deletedAtUtc)
            .Set(x => x.DeletedBy, actorId);

        UpdateResult result = await _collection.UpdateManyAsync(
            filter,
            update,
            cancellationToken: ct
        );
        return result.ModifiedCount;
    }
}
