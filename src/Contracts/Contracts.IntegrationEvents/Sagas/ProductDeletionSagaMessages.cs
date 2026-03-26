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
public sealed record ReviewsCascadeCompleted(Guid CorrelationId, int DeletedCount);

/// <summary>
/// Confirmation that files have been orphaned for the given products.
/// </summary>
public sealed record FilesCascadeCompleted(Guid CorrelationId, int DeletedCount);
