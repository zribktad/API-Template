namespace APITemplate.Application.Features.Bff.DTOs;

public sealed record BffUserResponse(
    string Sub,
    string PreferredUsername,
    string Email,
    string Name,
    string TenantId,
    IReadOnlyList<string> Roles);
