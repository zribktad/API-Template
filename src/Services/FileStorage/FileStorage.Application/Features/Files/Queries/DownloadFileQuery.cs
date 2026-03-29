using ErrorOr;
using FileStorage.Domain.Interfaces;
using SharedKernel.Application.DTOs;

namespace FileStorage.Application.Features.Files.Queries;

public sealed record DownloadFileQuery(Guid Id);

public sealed class DownloadFileQueryHandler
{
    public static Task<ErrorOr<FileDownloadInfo>> HandleAsync(
        DownloadFileQuery query,
        IStoredFileRepository repository,
        CancellationToken ct
    ) => StoredFileDownloadMetadata.ResolveAsync(query.Id, repository, ct);
}
