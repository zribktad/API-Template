namespace APITemplate.Application.Features.Examples.DTOs;

/// <summary>Stored-file metadata; the API opens the stream after a successful Wolverine query.</summary>
public sealed record FileDownloadInfo(string StoragePath, string ContentType, string FileName);
