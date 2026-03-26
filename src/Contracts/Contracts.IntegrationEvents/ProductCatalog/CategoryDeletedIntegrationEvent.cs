namespace Contracts.IntegrationEvents.ProductCatalog;

/// <summary>
/// Published by Product Catalog service when a category is soft-deleted.
/// </summary>
public sealed record CategoryDeletedIntegrationEvent(
    Guid CategoryId,
    Guid TenantId,
    DateTime OccurredAtUtc
);
