using ErrorOr;
using FileStorage.Application.Common.Contracts;
using FileStorage.Application.Common.Errors;
using FileStorage.Application.Common.Options;
using FileStorage.Application.Features.Files.DTOs;
using FileStorage.Domain.Entities;
using FileStorage.Domain.Interfaces;
using Microsoft.Extensions.Options;
using SharedKernel.Domain.Interfaces;

namespace FileStorage.Application.Features.Files.Commands;

public sealed record UploadFileCommand(UploadFileRequest Request);

public sealed class UploadFileCommandHandler
{
    public static async Task<ErrorOr<FileUploadResponse>> HandleAsync(
        UploadFileCommand command,
        IStoredFileRepository repository,
        IFileStorageService storage,
        IUnitOfWork unitOfWork,
        IOptions<FileStorageOptions> options,
        CancellationToken ct
    )
    {
        UploadFileRequest req = command.Request;
        FileStorageOptions opts = options.Value;
        string? extension = Path.GetExtension(req.FileName)?.ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !opts.AllowedExtensions.Contains(extension))
            return DomainErrors.Files.InvalidFileType(extension ?? "none");

        if (req.SizeBytes > opts.MaxFileSizeBytes)
            return DomainErrors.Files.FileTooLarge(opts.MaxFileSizeBytes);

        FileStorageResult storageResult = await storage.SaveAsync(req.FileStream, req.FileName, ct);

        try
        {
            StoredFile entity = new StoredFile
            {
                Id = Guid.NewGuid(),
                OriginalFileName = req.FileName,
                StoragePath = storageResult.StoragePath,
                ContentType = req.ContentType,
                SizeBytes = storageResult.SizeBytes,
                Description = req.Description,
            };

            await unitOfWork.ExecuteInTransactionAsync(
                async () =>
                {
                    await repository.AddAsync(entity, ct);
                },
                ct
            );

            return new FileUploadResponse(
                entity.Id,
                entity.OriginalFileName,
                entity.ContentType,
                entity.SizeBytes,
                entity.Description,
                entity.Audit.CreatedAtUtc
            );
        }
        catch
        {
            await storage.DeleteAsync(storageResult.StoragePath, CancellationToken.None);
            throw;
        }
    }
}
