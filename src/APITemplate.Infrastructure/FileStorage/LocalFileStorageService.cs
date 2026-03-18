using APITemplate.Application.Common.Context;
using APITemplate.Application.Common.Contracts;
using APITemplate.Application.Common.Options;
using Microsoft.Extensions.Options;

namespace APITemplate.Infrastructure.FileStorage;

public sealed class LocalFileStorageService : IFileStorageService
{
    private readonly FileStorageOptions _options;
    private readonly ITenantProvider _tenantProvider;

    public LocalFileStorageService(
        IOptions<FileStorageOptions> options,
        ITenantProvider tenantProvider
    )
    {
        _options = options.Value;
        _tenantProvider = tenantProvider;
    }

    public async Task<FileStorageResult> SaveAsync(
        Stream fileStream,
        string fileName,
        CancellationToken ct = default
    )
    {
        var tenantDir = Path.Combine(_options.BasePath, _tenantProvider.TenantId.ToString());
        Directory.CreateDirectory(tenantDir);

        var safeExtension = Path.GetExtension(Path.GetFileName(fileName));
        var storedFileName = $"{Guid.NewGuid()}{safeExtension}";
        var storagePath = Path.Combine(tenantDir, storedFileName);

        ValidatePathWithinBasePath(storagePath);

        long sizeBytes;
        await using (
            var output = new FileStream(
                storagePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                4096,
                FileOptions.Asynchronous
            )
        )
        {
            await fileStream.CopyToAsync(output, ct);
            sizeBytes = output.Length;
        }

        return new FileStorageResult(storagePath, sizeBytes);
    }

    public Task<Stream?> OpenReadAsync(string storagePath, CancellationToken ct = default)
    {
        ValidatePathWithinBasePath(storagePath);

        if (!File.Exists(storagePath))
            return Task.FromResult<Stream?>(null);

        return Task.FromResult<Stream?>(
            new FileStream(
                storagePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                4096,
                FileOptions.Asynchronous
            )
        );
    }

    public Task DeleteAsync(string storagePath, CancellationToken ct = default)
    {
        ValidatePathWithinBasePath(storagePath);

        if (File.Exists(storagePath))
            File.Delete(storagePath);

        return Task.CompletedTask;
    }

    private void ValidatePathWithinBasePath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var fullBasePath =
            Path.GetFullPath(_options.BasePath).TrimEnd(Path.DirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(fullBasePath, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Path traversal detected: access denied.");
    }
}
