using Identity.Application.Sagas;
using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Infrastructure.Persistence;

namespace Identity.Infrastructure.Persistence;

/// <summary>
/// EF Core context for Identity &amp; Tenancy microservice.
/// Enforces multi-tenancy, audit stamping, soft delete, and optimistic concurrency.
/// </summary>
public sealed class IdentityDbContext : TenantAuditableDbContext
{
    public IdentityDbContext(
        DbContextOptions<IdentityDbContext> options,
        TenantAuditableDbContextDependencies deps
    )
        : base(options, deps) { }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<TenantInvitation> TenantInvitations => Set<TenantInvitation>();
    public DbSet<TenantDeactivationSaga> TenantDeactivationSagas => Set<TenantDeactivationSaga>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);

        // Global query filters for multi-tenancy and soft-delete
        modelBuilder
            .Entity<AppUser>()
            .HasQueryFilter(e => (!HasTenant || e.TenantId == CurrentTenantId) && !e.IsDeleted);

        modelBuilder
            .Entity<Tenant>()
            .HasQueryFilter(e => (!HasTenant || e.TenantId == CurrentTenantId) && !e.IsDeleted);

        modelBuilder
            .Entity<TenantInvitation>()
            .HasQueryFilter(e => (!HasTenant || e.TenantId == CurrentTenantId) && !e.IsDeleted);
    }
}
