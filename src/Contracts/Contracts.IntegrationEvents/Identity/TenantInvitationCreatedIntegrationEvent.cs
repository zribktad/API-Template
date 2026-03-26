namespace Contracts.IntegrationEvents.Identity;

/// <summary>
/// Published by Identity service when a tenant invitation is created.
/// </summary>
public sealed record TenantInvitationCreatedIntegrationEvent(
    Guid InvitationId,
    string Email,
    string TenantName,
    string Token,
    DateTime OccurredAtUtc
);
