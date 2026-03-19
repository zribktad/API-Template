using APITemplate.Application.Common.Contracts;
using APITemplate.Application.Common.Errors;
using APITemplate.Application.Common.Extensions;
using APITemplate.Application.Features.Examples.DTOs;
using APITemplate.Domain.Entities;
using APITemplate.Domain.Exceptions;
using APITemplate.Domain.Interfaces;
using MediatR;

namespace APITemplate.Application.Features.Examples.Handlers;

/// <summary>Requests the download of the stored file identified by the inner <see cref="DownloadFileRequest"/>.</summary>
public sealed record DownloadFileQuery(DownloadFileRequest Request) : IRequest<FileDownloadResult>;

/// <summary>
/// Carries the open file stream together with its content type and original file name, ready for the presentation layer to write to the HTTP response.
/// </summary>
public sealed record FileDownloadResult(Stream FileStream, string ContentType, string FileName);

/// <summary>
/// Application-layer handler that resolves a stored file record and opens its physical stream for download, throwing <see cref="NotFoundException"/> when the record or the physical file is absent.
/// </summary>
public sealed class DownloadFileHandler : IRequestHandler<DownloadFileQuery, FileDownloadResult>
{
    private readonly IStoredFileRepository _repository;
    private readonly IFileStorageService _storage;

    public DownloadFileHandler(IStoredFileRepository repository, IFileStorageService storage)
    {
        _repository = repository;
        _storage = storage;
    }

    /// <summary>Looks up the stored file entity and opens the backing storage stream, returning a <see cref="FileDownloadResult"/> for the presentation layer.</summary>
    public async Task<FileDownloadResult> Handle(DownloadFileQuery query, CancellationToken ct)
    {
        var entity = await _repository.GetByIdOrThrowAsync(
            query.Request.Id,
            ErrorCatalog.Examples.FileNotFound,
            ct
        );

        var stream =
            await _storage.OpenReadAsync(entity.StoragePath, ct)
            ?? throw new NotFoundException(
                nameof(StoredFile),
                query.Request.Id,
                ErrorCatalog.Examples.FileNotFound
            );

        return new FileDownloadResult(stream, entity.ContentType, entity.OriginalFileName);
    }
}
