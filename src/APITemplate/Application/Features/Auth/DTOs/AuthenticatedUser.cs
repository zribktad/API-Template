using APITemplate.Domain.Enums;

namespace APITemplate.Application.Features.Auth.DTOs;

public sealed record AuthenticatedUser(
    Guid UserId,
    Guid TenantId,
    string Username,
    UserRole Role);
