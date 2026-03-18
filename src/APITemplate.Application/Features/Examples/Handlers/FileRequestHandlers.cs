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

public sealed record UploadFileCommand(
    Stream FileStream,
    string FileName,
    string ContentType,
    long SizeBytes,
    string? Description
) : IRequest<FileUploadResponse>;

public sealed record DownloadFileQuery(Guid Id) : IRequest<FileDownloadResult?>;

public sealed record FileDownloadResult(Stream FileStream, string ContentType, string FileName);

public sealed class FileRequestHandlers
    : IRequestHandler<UploadFileCommand, FileUploadResponse>,
        IRequestHandler<DownloadFileQuery, FileDownloadResult?>
{
    private readonly IStoredFileRepository _repository;
    private readonly IFileStorageService _storage;
    private readonly IUnitOfWork _unitOfWork;
    private readonly FileStorageOptions _options;

    public FileRequestHandlers(
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

    public async Task<FileUploadResponse> Handle(UploadFileCommand command, CancellationToken ct)
    {
        var extension = Path.GetExtension(command.FileName)?.ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !_options.AllowedExtensions.Contains(extension))
            throw new ValidationException(
                $"File type '{extension}' is not allowed.",
                ErrorCatalog.Examples.InvalidFileType
            );

        if (command.SizeBytes > _options.MaxFileSizeBytes)
            throw new ValidationException(
                $"File size exceeds the maximum allowed size of {_options.MaxFileSizeBytes} bytes.",
                ErrorCatalog.Examples.FileTooLarge
            );

        var storageResult = await _storage.SaveAsync(command.FileStream, command.FileName, ct);

        try
        {
            var entity = new StoredFile
            {
                Id = Guid.NewGuid(),
                OriginalFileName = command.FileName,
                StoragePath = storageResult.StoragePath,
                ContentType = command.ContentType,
                SizeBytes = storageResult.SizeBytes,
                Description = command.Description,
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

    public async Task<FileDownloadResult?> Handle(DownloadFileQuery query, CancellationToken ct)
    {
        var entity =
            await _repository.GetByIdAsync(query.Id, ct)
            ?? throw new NotFoundException(
                nameof(StoredFile),
                query.Id,
                ErrorCatalog.Examples.FileNotFound
            );

        var stream =
            await _storage.OpenReadAsync(entity.StoragePath, ct)
            ?? throw new NotFoundException(
                nameof(StoredFile),
                query.Id,
                ErrorCatalog.Examples.FileNotFound
            );

        return new FileDownloadResult(stream, entity.ContentType, entity.OriginalFileName);
    }
}
