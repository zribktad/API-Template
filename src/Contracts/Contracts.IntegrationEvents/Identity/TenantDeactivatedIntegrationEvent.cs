namespace Contracts.IntegrationEvents.Identity;

/// <summary>
/// Published by Identity service when a tenant is deactivated (soft-deleted).
/// All dependent services should cascade their cleanup.
/// CorrelationId links replies back to the originating TenantDeactivationSaga instance.
/// </summary>
public sealed record TenantDeactivatedIntegrationEvent(
    Guid CorrelationId,
    Guid TenantId,
    Guid ActorId,
    DateTime OccurredAtUtc
);
