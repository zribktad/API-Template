namespace Contracts.IntegrationEvents.ProductCatalog;

/// <summary>
/// Published by Product Catalog service when a new product is created.
/// </summary>
public sealed record ProductCreatedIntegrationEvent(
    Guid ProductId,
    Guid TenantId,
    string Name,
    DateTime OccurredAtUtc
);
