using Microsoft.Extensions.Logging;
using Npgsql;

namespace SharedKernel.Infrastructure.Startup;

internal sealed class AdvisoryLockLease : IAsyncDisposable
{
    private readonly NpgsqlConnection _connection;
    private readonly long _lockKey;
    private readonly ILogger _logger;
    private bool _disposed;

    internal AdvisoryLockLease(NpgsqlConnection connection, long lockKey, ILogger logger)
    {
        _connection = connection;
        _lockKey = lockKey;
        _logger = logger;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            await using NpgsqlCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT pg_advisory_unlock(@lockKey)";
            cmd.Parameters.AddWithValue("lockKey", _lockKey);
            await cmd.ExecuteNonQueryAsync();

            _logger.LogDebug("Released advisory lock {LockKey}", _lockKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to release advisory lock {LockKey}. Lock will be released when the connection closes.",
                _lockKey
            );
        }
        finally
        {
            await _connection.DisposeAsync();
        }
    }
}
