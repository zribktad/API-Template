using APITemplate.Application.Common.Contracts;
using APITemplate.Application.Common.Errors;
using APITemplate.Application.Common.Options;
using APITemplate.Application.Features.Examples.DTOs;
using APITemplate.Domain.Entities;
using APITemplate.Domain.Exceptions;
using APITemplate.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace APITemplate.Application.Features.Examples;

public sealed record UploadFileCommand(UploadFileRequest Request);

public sealed class UploadFileCommandHandler
{
    public static async Task<FileUploadResponse> HandleAsync(
        UploadFileCommand command,
        IStoredFileRepository repository,
        IFileStorageService storage,
        IUnitOfWork unitOfWork,
        IOptions<FileStorageOptions> options,
        CancellationToken ct
    )
    {
        var req = command.Request;
        var opts = options.Value;
        var extension = Path.GetExtension(req.FileName)?.ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !opts.AllowedExtensions.Contains(extension))
            throw new ValidationException(
                $"File type '{extension}' is not allowed.",
                ErrorCatalog.Examples.InvalidFileType
            );

        if (req.SizeBytes > opts.MaxFileSizeBytes)
            throw new ValidationException(
                $"File size exceeds the maximum allowed size of {opts.MaxFileSizeBytes} bytes.",
                ErrorCatalog.Examples.FileTooLarge
            );

        var storageResult = await storage.SaveAsync(req.FileStream, req.FileName, ct);

        try
        {
            var entity = new StoredFile
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
