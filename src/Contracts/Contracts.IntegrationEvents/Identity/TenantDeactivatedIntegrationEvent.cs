namespace Contracts.IntegrationEvents.Identity;

/// <summary>
/// Published by Identity service when a tenant is deactivated (soft-deleted).
/// All dependent services should cascade their cleanup.
/// </summary>
public sealed record TenantDeactivatedIntegrationEvent(
    Guid TenantId,
    Guid ActorId,
    DateTime OccurredAtUtc
);
