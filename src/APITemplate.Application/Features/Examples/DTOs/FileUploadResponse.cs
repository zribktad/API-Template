namespace APITemplate.Application.Features.Examples.DTOs;

public sealed record FileUploadResponse(
    Guid Id,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    string? Description,
    DateTime CreatedAtUtc
);
