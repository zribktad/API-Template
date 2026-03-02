using APITemplate.Domain.Entities;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace APITemplate.Infrastructure.Persistence;

public sealed class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        _database = client.GetDatabase(settings.Value.DatabaseName);
    }

    public IMongoDatabase Database => _database;

    public IMongoCollection<ProductData> ProductData
        => _database.GetCollection<ProductData>("product_data");
}
