using APITemplate.Application.Common.Contracts;
using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Common.Errors;
using APITemplate.Application.Common.Extensions;
using APITemplate.Application.Features.Examples.DTOs;
using APITemplate.Domain.Entities;
using APITemplate.Domain.Exceptions;
using APITemplate.Domain.Interfaces;

namespace APITemplate.Application.Features.Examples;

public sealed record DownloadFileQuery(DownloadFileRequest Request) : IQuery<FileDownloadResult>;

public sealed record FileDownloadResult(Stream FileStream, string ContentType, string FileName);

public sealed class DownloadFileQueryHandler : IQueryHandler<DownloadFileQuery, FileDownloadResult>
{
    private readonly IStoredFileRepository _repository;
    private readonly IFileStorageService _storage;

    public DownloadFileQueryHandler(IStoredFileRepository repository, IFileStorageService storage)
    {
        _repository = repository;
        _storage = storage;
    }

    public async Task<FileDownloadResult> HandleAsync(DownloadFileQuery query, CancellationToken ct)
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
