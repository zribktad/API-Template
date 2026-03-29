using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace SharedKernel.Infrastructure.Startup;

public sealed class PostgresAdvisoryLockStartupTaskCoordinator : IStartupTaskCoordinator
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PostgresAdvisoryLockStartupTaskCoordinator> _logger;

    public PostgresAdvisoryLockStartupTaskCoordinator(
        IConfiguration configuration,
        ILogger<PostgresAdvisoryLockStartupTaskCoordinator> logger
    )
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IAsyncDisposable> AcquireAsync(
        StartupTaskName task,
        CancellationToken cancellationToken = default
    )
    {
        long lockKey = (long)task;
        string connectionString = ResolveConnectionString();

        NpgsqlConnection connection = new(connectionString);
        await connection.OpenAsync(cancellationToken);

        _logger.LogDebug("Acquiring advisory lock {LockKey} for {Task}", lockKey, task);

        await using NpgsqlCommand cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT pg_advisory_lock(@lockKey)";
        cmd.Parameters.AddWithValue("lockKey", lockKey);
        await cmd.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation("Acquired advisory lock {LockKey} for {Task}", lockKey, task);

        return new AdvisoryLockLease(connection, lockKey, _logger);
    }

    private string ResolveConnectionString()
    {
        // Try common connection string names
        string? connectionString =
            _configuration.GetConnectionString("DefaultConnection")
            ?? _configuration.GetConnectionString("IdentityDb")
            ?? _configuration.GetConnectionString("ProductCatalogDb")
            ?? _configuration.GetConnectionString("ReviewsDb");

        return connectionString
            ?? throw new InvalidOperationException(
                "No PostgreSQL connection string found. Expected 'DefaultConnection', 'IdentityDb', 'ProductCatalogDb', or 'ReviewsDb'."
            );
    }
}
