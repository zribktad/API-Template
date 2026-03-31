using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SharedKernel.Infrastructure.Persistence;

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
            DesignTimeConnectionStringResolver.Resolve(
                "src/Services/FileStorage/FileStorage.Api",
                "FileStorageDb",
                args
            )
        );

        return new FileStorageDbContext(
            optionsBuilder.Options,
            DesignTimeDbContextDefaults.CreateDependencies(),
            DesignTimeDbContextDefaults.EntityNormalizationService
        );
    }
}
