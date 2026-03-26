namespace Contracts.IntegrationEvents.Identity;

/// <summary>
/// Published by Identity service when a new user successfully registers.
/// </summary>
public sealed record UserRegisteredIntegrationEvent(
    Guid UserId,
    Guid TenantId,
    string Email,
    string Username,
    DateTime OccurredAtUtc
);
