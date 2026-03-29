using APITemplate.Application.Common.Errors;
using APITemplate.Application.Common.Extensions;
using APITemplate.Domain.Interfaces;
using ErrorOr;
using SharedKernel.Application.DTOs;

namespace APITemplate.Application.Features.Examples;

public sealed record DownloadFileQuery(Guid Id);

public sealed class DownloadFileQueryHandler
{
    public static async Task<ErrorOr<FileDownloadInfo>> HandleAsync(
        DownloadFileQuery query,
        IStoredFileRepository repository,
        CancellationToken ct
    )
    {
        var entityResult = await repository.GetByIdOrError(
            query.Id,
            DomainErrors.Examples.FileNotFound(query.Id.ToString()),
            ct
        );
        if (entityResult.IsError)
            return entityResult.Errors;
        var entity = entityResult.Value;

        return new FileDownloadInfo(
            entity.StoragePath,
            entity.ContentType,
            entity.OriginalFileName
        );
    }
}
