using SharedKernel.Application.Context;
using SharedKernel.Infrastructure.Persistence.Auditing;
using SharedKernel.Infrastructure.Persistence.SoftDelete;

namespace SharedKernel.Infrastructure.Persistence;

/// <summary>
/// Groups the shared infrastructure dependencies required by <see cref="TenantAuditableDbContext"/>.
/// Register as scoped so the lifetime matches the contained services.
/// </summary>
public sealed record TenantAuditableDbContextDependencies(
    ITenantProvider TenantProvider,
    IActorProvider ActorProvider,
    TimeProvider TimeProvider,
    IEnumerable<ISoftDeleteCascadeRule> SoftDeleteCascadeRules,
    IAuditableEntityStateManager EntityStateManager,
    ISoftDeleteProcessor SoftDeleteProcessor
);
