using Wolverine;
using Wolverine.Persistence.Sagas;

namespace Contracts.IntegrationEvents.Sagas;

/// <summary>
/// Command to start the product deletion cascade saga.
/// </summary>
public sealed record StartProductDeletionSaga(
    Guid CorrelationId,
    IReadOnlyList<Guid> ProductIds,
    Guid TenantId,
    Guid ActorId
);

/// <summary>
/// Confirmation that reviews have been cascade-deleted for the given products.
/// </summary>
public sealed record ReviewsCascadeCompleted(
    [property: SagaIdentity] Guid CorrelationId,
    int DeletedCount
);

/// <summary>
/// Confirmation that files have been orphaned for the given products.
/// </summary>
public sealed record FilesCascadeCompleted(
    [property: SagaIdentity] Guid CorrelationId,
    int DeletedCount
);

/// <summary>
/// Timeout message for product deletion saga reliability.
/// </summary>
public sealed record ProductDeletionSagaTimeout([property: SagaIdentity] Guid CorrelationId)
    : TimeoutMessage(TimeSpan.FromMinutes(5));
