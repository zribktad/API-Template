namespace APITemplate.Application.Features.Examples.DTOs;

public sealed record UploadFileRequest(
    Stream FileStream,
    string FileName,
    string ContentType,
    long SizeBytes,
    string? Description
);
