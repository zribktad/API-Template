using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SharedKernel.Application.Context;
using SharedKernel.Domain.Entities.Contracts;
using SharedKernel.Infrastructure.Persistence.Auditing;
using SharedKernel.Infrastructure.Persistence.EntityNormalization;
using SharedKernel.Infrastructure.Persistence.SoftDelete;

namespace SharedKernel.Infrastructure.Persistence;

/// <summary>
/// Null-object collaborators used by EF Core design-time factories so migrations can be created
/// without the full runtime dependency graph.
/// </summary>
public static class DesignTimeDbContextDefaults
{
    public static TenantAuditableDbContextDependencies CreateDependencies() =>
        new(
            new NullTenantProvider(),
            new NullActorProvider(),
            TimeProvider.System,
            [],
            new NullAuditableEntityStateManager(),
            new NullSoftDeleteProcessor()
        );

    public static IEntityNormalizationService EntityNormalizationService { get; } =
        new NullEntityNormalizationService();

    private sealed class NullTenantProvider : ITenantProvider
    {
        public Guid TenantId => Guid.Empty;

        public bool HasTenant => false;
    }

    private sealed class NullActorProvider : IActorProvider
    {
        public Guid ActorId => Guid.Empty;
    }

    private sealed class NullEntityNormalizationService : IEntityNormalizationService
    {
        public void Normalize(IAuditableTenantEntity entity) { }
    }

    private sealed class NullAuditableEntityStateManager : IAuditableEntityStateManager
    {
        public void StampAdded(
            EntityEntry entry,
            IAuditableTenantEntity entity,
            DateTime now,
            Guid actor,
            bool hasTenant,
            Guid currentTenantId
        ) { }

        public void StampModified(IAuditableTenantEntity entity, DateTime now, Guid actor) { }

        public void MarkSoftDeleted(
            EntityEntry entry,
            IAuditableTenantEntity entity,
            DateTime now,
            Guid actor
        ) { }
    }

    private sealed class NullSoftDeleteProcessor : ISoftDeleteProcessor
    {
        public Task ProcessAsync(
            DbContext dbContext,
            EntityEntry entry,
            IAuditableTenantEntity entity,
            DateTime now,
            Guid actor,
            IReadOnlyCollection<ISoftDeleteCascadeRule> softDeleteCascadeRules,
            CancellationToken cancellationToken
        ) => Task.CompletedTask;
    }
}
