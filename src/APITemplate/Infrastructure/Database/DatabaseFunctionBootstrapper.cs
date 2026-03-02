using System.Reflection;
using APITemplate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace APITemplate.Infrastructure.Database;

/// <summary>
/// Applies all stored procedure / function SQL scripts at application startup.
///
/// How it works:
///   1. Scans the assembly for embedded resources under the
///      "APITemplate.Infrastructure.Database.Functions" namespace.
///   2. Reads each .sql file and executes it against the database.
///   3. Because every script uses CREATE OR REPLACE, execution is idempotent —
///      safe to run on every start, whether the function already exists or not.
///
/// Why not EF Core migrations?
///   Migrations track schema diffs (tables, columns, indexes).
///   Functions are versioned code, not schema state — they are always fully
///   replaced, never partially altered. Keeping them as standalone SQL files:
///     - Makes them readable by DBAs without navigating .cs migration files.
///     - Lets you modify a function by editing one .sql file (no new migration).
///     - Keeps migrations clean — only structural changes live there.
/// </summary>
public sealed class DatabaseFunctionBootstrapper
{
    private const string FunctionsNamespace = "APITemplate.Infrastructure.Database.Functions";

    private readonly AppDbContext _dbContext;
    private readonly ILogger<DatabaseFunctionBootstrapper> _logger;

    public DatabaseFunctionBootstrapper(
        AppDbContext dbContext,
        ILogger<DatabaseFunctionBootstrapper> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task ApplyAsync(CancellationToken ct = default)
    {
        var assembly = Assembly.GetExecutingAssembly();

        var sqlResources = assembly
            .GetManifestResourceNames()
            .Where(name => name.StartsWith(FunctionsNamespace) && name.EndsWith(".sql"))
            .OrderBy(name => name);

        foreach (var resourceName in sqlResources)
        {
            await using var stream = assembly.GetManifestResourceStream(resourceName)!;
            using var reader = new StreamReader(stream);
            var sql = await reader.ReadToEndAsync(ct);

            _logger.LogInformation("Applying database function: {Resource}", resourceName);
            await _dbContext.Database.ExecuteSqlRawAsync(sql, ct);
        }
    }
}
