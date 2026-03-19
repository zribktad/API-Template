using APITemplate.Application.Common.Contracts;
using APITemplate.Application.Common.Errors;
using APITemplate.Application.Common.Options;
using APITemplate.Application.Features.Examples.DTOs;
using APITemplate.Domain.Entities;
using APITemplate.Domain.Exceptions;
using APITemplate.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Options;

namespace APITemplate.Application.Features.Examples.Handlers;

/// <summary>Stores an uploaded file and persists its metadata as described by the inner <see cref="UploadFileRequest"/>.</summary>
public sealed record UploadFileCommand(UploadFileRequest Request) : IRequest<FileUploadResponse>;

/// <summary>
/// Application-layer handler that validates file type and size constraints, saves the file to storage, persists its metadata in a transaction, and rolls back the physical file on any persistence failure.
/// </summary>
public sealed class UploadFileHandler : IRequestHandler<UploadFileCommand, FileUploadResponse>
{
    private readonly IStoredFileRepository _repository;
    private readonly IFileStorageService _storage;
    private readonly IUnitOfWork _unitOfWork;
    private readonly FileStorageOptions _options;

    public UploadFileHandler(
        IStoredFileRepository repository,
        IFileStorageService storage,
        IUnitOfWork unitOfWork,
        IOptions<FileStorageOptions> options
    )
    {
        _repository = repository;
        _storage = storage;
        _unitOfWork = unitOfWork;
        _options = options.Value;
    }

    /// <summary>Validates extension and size, saves to storage, persists the entity record in a transaction, and cleans up the physical file if the database write fails.</summary>
    public async Task<FileUploadResponse> Handle(UploadFileCommand command, CancellationToken ct)
    {
        var req = command.Request;
        var extension = Path.GetExtension(req.FileName)?.ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !_options.AllowedExtensions.Contains(extension))
            throw new ValidationException(
                $"File type '{extension}' is not allowed.",
                ErrorCatalog.Examples.InvalidFileType
            );

        if (req.SizeBytes > _options.MaxFileSizeBytes)
            throw new ValidationException(
                $"File size exceeds the maximum allowed size of {_options.MaxFileSizeBytes} bytes.",
                ErrorCatalog.Examples.FileTooLarge
            );

        var storageResult = await _storage.SaveAsync(req.FileStream, req.FileName, ct);

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

            await _unitOfWork.ExecuteInTransactionAsync(
                async () =>
                {
                    await _repository.AddAsync(entity, ct);
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
            await _storage.DeleteAsync(storageResult.StoragePath, CancellationToken.None);
            throw;
        }
    }
}
