using Npgsql;

namespace TestCommon;

public static class TestDatabaseLifecycle
{
    public static string BuildConnectionString(string serverConnectionString, string databaseName)
    {
        NpgsqlConnectionStringBuilder builder = new(serverConnectionString)
        {
            Database = databaseName,
        };
        return builder.ConnectionString;
    }

    public static async Task CreateDatabaseAsync(string serverConnectionString, string databaseName)
    {
        await using NpgsqlConnection conn = new(serverConnectionString);
        await conn.OpenAsync();
        await using NpgsqlCommand cmd = conn.CreateCommand();
        cmd.CommandText = $"CREATE DATABASE \"{databaseName}\"";
        await cmd.ExecuteNonQueryAsync();
    }

    public static async Task DropDatabaseAsync(string serverConnectionString, string databaseName)
    {
        try
        {
            await using NpgsqlConnection conn = new(serverConnectionString);
            await conn.OpenAsync();
            await using NpgsqlCommand cmd = conn.CreateCommand();
            // databaseName is always a GUID — no SQL injection risk
            cmd.CommandText = $"""
                SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '{databaseName}' AND pid <> pg_backend_pid();
                DROP DATABASE IF EXISTS "{databaseName}";
                """;
            await cmd.ExecuteNonQueryAsync();
        }
        catch
        {
            // Best-effort cleanup — container disposal handles the rest.
        }
    }
}
