using FileStorage.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Infrastructure.Persistence;
using SharedKernel.Infrastructure.Persistence.EntityNormalization;

namespace FileStorage.Infrastructure.Persistence;

/// <summary>
/// EF Core context for FileStorage microservice.
/// Enforces multi-tenancy, audit stamping, soft delete, and optimistic concurrency.
/// </summary>
public sealed class FileStorageDbContext : TenantAuditableDbContext
{
    public FileStorageDbContext(
        DbContextOptions<FileStorageDbContext> options,
        TenantAuditableDbContextDependencies deps,
        IEntityNormalizationService? entityNormalizationService = null
    )
        : base(options, deps, entityNormalizationService) { }

    public DbSet<StoredFile> StoredFiles => Set<StoredFile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FileStorageDbContext).Assembly);

        // Global query filters for multi-tenancy and soft-delete
        modelBuilder
            .Entity<StoredFile>()
            .HasQueryFilter(e => (!HasTenant || e.TenantId == CurrentTenantId) && !e.IsDeleted);
    }
}
