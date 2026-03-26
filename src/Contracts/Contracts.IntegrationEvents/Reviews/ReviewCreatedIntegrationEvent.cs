namespace Contracts.IntegrationEvents.Reviews;

/// <summary>
/// Published by Reviews service when a new product review is created.
/// </summary>
public sealed record ReviewCreatedIntegrationEvent(
    Guid ReviewId,
    Guid ProductId,
    Guid UserId,
    Guid TenantId,
    int Rating,
    DateTime OccurredAtUtc
);
