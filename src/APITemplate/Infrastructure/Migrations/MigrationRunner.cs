using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace APITemplate.Infrastructure.Migrations;

public sealed class MigrationRunner
{
    private const string MigrationsCollection = "_migrations";

    private readonly IMongoDatabase _database;
    private readonly IReadOnlyList<IMigration> _migrations;
    private readonly ILogger<MigrationRunner> _logger;

    /// <summary>Production constructor — discovers migrations from assembly automatically.</summary>
    public MigrationRunner(IMongoDatabase database, ILogger<MigrationRunner> logger)
        : this(database, logger, DiscoverMigrations()) { }

    /// <summary>Internal constructor for unit tests — accepts injected migrations.</summary>
    internal MigrationRunner(IMongoDatabase database, ILogger<MigrationRunner> logger, IReadOnlyList<IMigration> migrations)
    {
        _database = database;
        _logger = logger;
        _migrations = migrations;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        var collection = _database.GetCollection<MongoMigrationRecord>(MigrationsCollection);

        // FindAsync is an interface method (not extension) — fully mockable in tests.
        var cursor = await collection.FindAsync(FilterDefinition<MongoMigrationRecord>.Empty, cancellationToken: ct);
        var applied = await cursor.ToListAsync(ct);
        var appliedVersions = applied.Select(x => x.Version).ToHashSet();

        var pending = _migrations
            .Where(m => !appliedVersions.Contains(m.Version))
            .OrderBy(m => m.Version)
            .ToList();

        if (pending.Count == 0)
        {
            _logger.LogInformation("MongoDB: no pending migrations");
            return;
        }

        foreach (var migration in pending)
        {
            _logger.LogInformation("MongoDB: applying migration {Version} — {Description}",
                migration.Version, migration.Description);

            await migration.UpAsync(_database, ct);

            await collection.InsertOneAsync(new MongoMigrationRecord
            {
                Version = migration.Version,
                Description = migration.Description,
                AppliedAt = DateTime.UtcNow
            }, cancellationToken: ct);

            _logger.LogInformation("MongoDB: migration {Version} applied", migration.Version);
        }
    }

    /// <summary>
    /// Discovers all IMigration implementations in this assembly — no DI registration needed.
    /// Adding a new migration class is enough; the runner picks it up automatically.
    /// </summary>
    private static IReadOnlyList<IMigration> DiscoverMigrations() =>
        typeof(IMigration).Assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsClass: true } && t.IsAssignableTo(typeof(IMigration)))
            .Select(t => (IMigration)Activator.CreateInstance(t)!)
            .OrderBy(m => m.Version)
            .ToList();
}
