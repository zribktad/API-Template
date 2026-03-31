using System.ComponentModel.DataAnnotations;

namespace ProductCatalog.Infrastructure.Persistence;

/// <summary>
/// Strongly-typed settings for the MongoDB connection, bound from the application configuration.
/// </summary>
public sealed class MongoDbSettings
{
    public const string SectionName = "MongoDB";

    [Required]
    public string ConnectionString { get; init; } = string.Empty;

    [Required]
    public string DatabaseName { get; init; } = string.Empty;
}
