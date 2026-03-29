using ErrorOr;
using FileStorage.Application.Common.Contracts;
using FileStorage.Application.Features.Files.Commands;
using FileStorage.Application.Features.Files.DTOs;
using FileStorage.Application.Features.Files.Queries;
using FileStorage.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SharedKernel.Api.Authorization;
using SharedKernel.Api.ErrorOrMapping;
using SharedKernel.Application.Common.Events;
using SharedKernel.Application.Security;
using SharedKernel.Domain.Interfaces;
using Wolverine;
using Wolverine.Http;

namespace FileStorage.Api.Endpoints;

/// <summary>Wolverine HTTP endpoints for multipart upload and file download (local storage).</summary>
public static class FilesHttpEndpoints
{
    [WolverinePost("/api/v1/files/upload")]
    [RequirePermission(Permission.Files.Upload)]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public static async Task<IResult> UploadAsync(
        [FromForm(Name = "File")] IFormFile file,
        [FromForm(Name = "Description")] string? description,
        IMessageBus bus,
        HttpContext httpContext,
        CancellationToken ct
    )
    {
        await using Stream stream = file.OpenReadStream();
        ErrorOr<FileUploadResponse> result = await bus.InvokeAsync<ErrorOr<FileUploadResponse>>(
            new UploadFileCommand(
                new UploadFileRequest(
                    stream,
                    file.FileName,
                    file.ContentType,
                    file.Length,
                    description
                )
            ),
            ct
        );

        return result.ToIResult(
            httpContext,
            value => Results.Created($"/api/v1/files/{value.Id}/download", value)
        );
    }

    [WolverineGet("/api/v1/files/{id:guid}/download")]
    [RequirePermission(Permission.Files.Download)]
    [OutputCache(PolicyName = CacheTags.Files)]
    public static async Task<IResult> DownloadAsync(
        Guid id,
        HttpContext httpContext,
        CancellationToken ct
    )
    {
        // Resolve dependencies inside the method body so Wolverine.HTTP codegen does not inject domain types into generated frames.
        IStoredFileRepository repository =
            httpContext.RequestServices.GetRequiredService<IStoredFileRepository>();
        IFileStorageService fileStorage =
            httpContext.RequestServices.GetRequiredService<IFileStorageService>();

        ErrorOr<FileDownloadInfo> meta = await StoredFileDownloadMetadata.ResolveAsync(
            id,
            repository,
            ct
        );
        if (meta.IsError)
            return meta.Errors.ToProblemDetailsIResult(httpContext);

        FileDownloadInfo info = meta.Value;

        Stream? stream = await fileStorage.OpenReadAsync(info.StoragePath, ct);
        if (stream is null)
            return Results.NotFound();

        try
        {
            return Results.File(stream, info.ContentType, info.FileName);
        }
        catch
        {
            await stream.DisposeAsync();
            throw;
        }
    }
}
