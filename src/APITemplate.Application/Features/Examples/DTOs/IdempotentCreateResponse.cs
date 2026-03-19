namespace APITemplate.Application.Features.Examples.DTOs;

public sealed record IdempotentCreateResponse(
    Guid Id,
    string Name,
    string? Description,
    DateTime CreatedAtUtc
);
