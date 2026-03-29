namespace SharedKernel.Infrastructure.Startup;

/// <summary>
/// Startup task identifiers used as stable PostgreSQL advisory lock keys.
/// </summary>
public enum StartupTaskName
{
    DatabaseMigration = 1,
    DataSeeding = 2,
    RecurringJobSync = 3,
}
