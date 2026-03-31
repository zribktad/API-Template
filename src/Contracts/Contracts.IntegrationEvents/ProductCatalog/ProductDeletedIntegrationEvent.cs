namespace Contracts.IntegrationEvents.ProductCatalog;

/// <summary>
/// Published by Product Catalog service when products are soft-deleted.
/// Triggers cascade in Reviews (delete reviews) and File Storage (orphan files).
/// </summary>
public sealed record ProductDeletedIntegrationEvent(
    Guid CorrelationId,
    IReadOnlyList<Guid> ProductIds,
    Guid TenantId,
    DateTime OccurredAtUtc
);
