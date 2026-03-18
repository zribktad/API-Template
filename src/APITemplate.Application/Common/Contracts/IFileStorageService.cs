namespace APITemplate.Application.Common.Contracts;

public interface IFileStorageService
{
    Task<FileStorageResult> SaveAsync(
        Stream fileStream,
        string fileName,
        CancellationToken ct = default
    );
    Task<Stream?> OpenReadAsync(string storagePath, CancellationToken ct = default);
    Task DeleteAsync(string storagePath, CancellationToken ct = default);
}

public sealed record FileStorageResult(string StoragePath, long SizeBytes);
