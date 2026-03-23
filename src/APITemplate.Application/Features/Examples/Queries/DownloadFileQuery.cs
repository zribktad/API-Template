using APITemplate.Application.Common.Contracts;
using APITemplate.Application.Common.Errors;
using APITemplate.Application.Common.Extensions;
using APITemplate.Application.Features.Examples.DTOs;
using APITemplate.Domain.Entities;
using APITemplate.Domain.Exceptions;
using APITemplate.Domain.Interfaces;

namespace APITemplate.Application.Features.Examples;

public sealed record DownloadFileQuery(DownloadFileRequest Request);

public sealed record FileDownloadResult(Stream FileStream, string ContentType, string FileName);

public sealed class DownloadFileQueryHandler
{
    public static async Task<FileDownloadResult> HandleAsync(
        DownloadFileQuery query,
        IStoredFileRepository repository,
        IFileStorageService storage,
        CancellationToken ct
    )
    {
        var entity = await repository.GetByIdOrThrowAsync(
            query.Request.Id,
            ErrorCatalog.Examples.FileNotFound,
            ct
        );

        var stream =
            await storage.OpenReadAsync(entity.StoragePath, ct)
            ?? throw new NotFoundException(
                nameof(StoredFile),
                query.Request.Id,
                ErrorCatalog.Examples.FileNotFound
            );

        return new FileDownloadResult(stream, entity.ContentType, entity.OriginalFileName);
    }
}
