using ErrorOr;
using FileStorage.Application.Common.Contracts;
using FileStorage.Application.Common.Errors;
using FileStorage.Application.Extensions;
using FileStorage.Application.Features.Files.DTOs;
using FileStorage.Domain.Interfaces;

namespace FileStorage.Application.Features.Files.Queries;

public sealed record DownloadFileQuery(DownloadFileRequest Request);

public sealed record FileDownloadResult(Stream FileStream, string ContentType, string FileName);

public sealed class DownloadFileQueryHandler
{
    public static async Task<ErrorOr<FileDownloadResult>> HandleAsync(
        DownloadFileQuery query,
        IStoredFileRepository repository,
        IFileStorageService storage,
        CancellationToken ct
    )
    {
        ErrorOr<Domain.Entities.StoredFile> entityResult = await repository.GetByIdOrError(
            query.Request.Id,
            DomainErrors.Files.NotFound(query.Request.Id.ToString()),
            ct
        );
        if (entityResult.IsError)
            return entityResult.Errors;
        Domain.Entities.StoredFile entity = entityResult.Value;

        Stream? stream = await storage.OpenReadAsync(entity.StoragePath, ct);
        if (stream is null)
            return DomainErrors.Files.NotFound(entity.OriginalFileName);

        return new FileDownloadResult(stream, entity.ContentType, entity.OriginalFileName);
    }
}
