using Wolverine.Persistence.Sagas;

namespace Contracts.IntegrationEvents.Sagas;

/// <summary>
/// Command to start the tenant deactivation cascade saga.
/// </summary>
public sealed record StartTenantDeactivationSaga(Guid CorrelationId, Guid TenantId, Guid ActorId);

/// <summary>
/// Confirmation that users have been deactivated for the given tenant.
/// </summary>
public sealed record UsersCascadeCompleted(
    [property: SagaIdentity] Guid CorrelationId,
    Guid TenantId,
    int DeactivatedCount
);

/// <summary>
/// Confirmation that products have been cascade-deleted for the given tenant.
/// </summary>
public sealed record ProductsCascadeCompleted(
    [property: SagaIdentity] Guid CorrelationId,
    Guid TenantId,
    int DeletedCount
);

/// <summary>
/// Confirmation that categories have been cascade-deleted for the given tenant.
/// </summary>
public sealed record CategoriesCascadeCompleted(
    [property: SagaIdentity] Guid CorrelationId,
    Guid TenantId,
    int DeletedCount
);
