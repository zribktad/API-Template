namespace Contracts.IntegrationEvents.Identity;

/// <summary>
/// Published by Identity service when a user's role is changed.
/// </summary>
public sealed record UserRoleChangedIntegrationEvent(
    Guid UserId,
    Guid TenantId,
    string Email,
    string Username,
    string OldRole,
    string NewRole,
    DateTime OccurredAtUtc
);
