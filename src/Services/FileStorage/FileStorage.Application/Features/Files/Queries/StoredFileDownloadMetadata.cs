using ErrorOr;
using FileStorage.Application.Common.Errors;
using FileStorage.Domain.Interfaces;
using SharedKernel.Application.DTOs;
using SharedKernel.Application.Extensions;

namespace FileStorage.Application.Features.Files.Queries;

/// <summary>Shared lookup of stored-file metadata for HTTP and message handlers.</summary>
public static class StoredFileDownloadMetadata
{
    public static async Task<ErrorOr<FileDownloadInfo>> ResolveAsync(
        Guid id,
        IStoredFileRepository repository,
        CancellationToken ct
    )
    {
        ErrorOr<Domain.Entities.StoredFile> entityResult = await repository.GetByIdOrError(
            id,
            DomainErrors.Files.NotFound(id.ToString()),
            ct
        );
        if (entityResult.IsError)
            return entityResult.Errors;

        Domain.Entities.StoredFile entity = entityResult.Value;
        return new FileDownloadInfo(
            entity.StoragePath,
            entity.ContentType,
            entity.OriginalFileName
        );
    }
}
