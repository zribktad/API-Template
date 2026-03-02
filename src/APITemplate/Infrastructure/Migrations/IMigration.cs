using MongoDB.Driver;

namespace APITemplate.Infrastructure.Migrations;

public interface IMigration
{
    int Version { get; }
    string Description { get; }
    Task UpAsync(IMongoDatabase database, CancellationToken ct = default);
}
