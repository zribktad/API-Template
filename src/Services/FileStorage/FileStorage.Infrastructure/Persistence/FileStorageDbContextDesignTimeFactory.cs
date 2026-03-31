using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FileStorage.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by EF Core tooling to create the DbContext
/// for migration scaffolding without requiring the full runtime DI container.
/// </summary>
public sealed class FileStorageDbContextDesignTimeFactory
    : IDesignTimeDbContextFactory<FileStorageDbContext>
{
    public FileStorageDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<FileStorageDbContext> optionsBuilder = new();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=file_storage_db;Username=postgres;Password=postgres"
        );

        return new FileStorageDbContext(
            optionsBuilder.Options,
            tenantProvider: null!,
            actorProvider: null!,
            timeProvider: TimeProvider.System,
            softDeleteCascadeRules: [],
            entityStateManager: null!,
            softDeleteProcessor: null!
        );
    }
}
