using APITemplate.Application.Common.Contracts;
using APITemplate.Application.Common.Errors;
using APITemplate.Application.Features.Examples.DTOs;
using APITemplate.Domain.Entities;
using APITemplate.Domain.Exceptions;
using APITemplate.Domain.Interfaces;
using MediatR;

namespace APITemplate.Application.Features.Examples.Handlers;

public sealed record DownloadFileQuery(DownloadFileRequest Request) : IRequest<FileDownloadResult>;

public sealed record FileDownloadResult(Stream FileStream, string ContentType, string FileName);

public sealed class DownloadFileHandler : IRequestHandler<DownloadFileQuery, FileDownloadResult>
{
    private readonly IStoredFileRepository _repository;
    private readonly IFileStorageService _storage;

    public DownloadFileHandler(IStoredFileRepository repository, IFileStorageService storage)
    {
        _repository = repository;
        _storage = storage;
    }

    public async Task<FileDownloadResult> Handle(DownloadFileQuery query, CancellationToken ct)
    {
        var entity =
            await _repository.GetByIdAsync(query.Request.Id, ct)
            ?? throw new NotFoundException(
                nameof(StoredFile),
                query.Request.Id,
                ErrorCatalog.Examples.FileNotFound
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
