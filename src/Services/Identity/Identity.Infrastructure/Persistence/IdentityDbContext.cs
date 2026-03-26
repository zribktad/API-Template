using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Application.Context;
using SharedKernel.Infrastructure.Persistence;
using SharedKernel.Infrastructure.Persistence.Auditing;
using SharedKernel.Infrastructure.Persistence.EntityNormalization;
using SharedKernel.Infrastructure.Persistence.SoftDelete;

namespace Identity.Infrastructure.Persistence;

/// <summary>
/// EF Core context for Identity & Tenancy microservice.
/// Enforces multi-tenancy, audit stamping, soft delete, and optimistic concurrency.
/// </summary>
public sealed class IdentityDbContext : TenantAuditableDbContext
{
    public IdentityDbContext(
        DbContextOptions<IdentityDbContext> options,
        ITenantProvider tenantProvider,
        IActorProvider actorProvider,
        TimeProvider timeProvider,
        IEnumerable<ISoftDeleteCascadeRule> softDeleteCascadeRules,
        IAuditableEntityStateManager entityStateManager,
        ISoftDeleteProcessor softDeleteProcessor,
        IEntityNormalizationService? entityNormalizationService = null
    )
        : base(
            options,
            tenantProvider,
            actorProvider,
            timeProvider,
            softDeleteCascadeRules,
            entityStateManager,
            softDeleteProcessor,
            entityNormalizationService
        ) { }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<TenantInvitation> TenantInvitations => Set<TenantInvitation>();

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
