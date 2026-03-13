using APITemplate.Domain.Enums;

namespace APITemplate.Application.Features.TenantInvitation.DTOs;

public sealed record TenantInvitationResponse(
    Guid Id,
    string Email,
    InvitationStatus Status,
    DateTime ExpiresAtUtc,
    DateTime CreatedAtUtc
);
