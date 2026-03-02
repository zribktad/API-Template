using MongoDB.Bson.Serialization.Attributes;

namespace APITemplate.Infrastructure.Migrations;

/// <summary>Stored in the "_migrations" collection — equivalent to EF Core's __EFMigrationsHistory.</summary>
public sealed class MongoMigrationRecord
{
    [BsonId]
    public int Version { get; init; }
    public string Description { get; init; } = string.Empty;
    public DateTime AppliedAt { get; init; }
}
