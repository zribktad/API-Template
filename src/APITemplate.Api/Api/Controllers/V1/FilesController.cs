using APITemplate.Api.Authorization;
using APITemplate.Api.Controllers;
using APITemplate.Api.Requests;
using APITemplate.Application.Common.Security;
using APITemplate.Application.Features.Examples.DTOs;
using APITemplate.Application.Features.Examples.Handlers;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace APITemplate.Api.Controllers.V1;

[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/files")]
/// <summary>
/// Presentation-layer controller that demonstrates multipart file upload and streamed download
/// using local file storage, limited to 10 MB per upload request.
/// </summary>
public sealed class FilesController : ApiControllerBase
{
    private readonly ISender _sender;

    public FilesController(ISender sender) => _sender = sender;

    /// <summary>
    /// Accepts a multipart form upload, streams the file to local storage via the application
    /// layer, and returns a 201 with a Location header pointing to the download endpoint.
    /// </summary>
    [HttpPost("upload")]
    [RequirePermission(Permission.Examples.Upload)]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<FileUploadResponse>> Upload(
        [FromForm] FileUploadRequest request,
        CancellationToken ct
    )
    {
        await using var stream = request.File.OpenReadStream();
        var result = await _sender.Send(
            new UploadFileCommand(
                new UploadFileRequest(
                    stream,
                    request.File.FileName,
                    request.File.ContentType,
                    request.File.Length,
                    request.Description
                )
            ),
            ct
        );
        return CreatedAtAction(
            nameof(Download),
            new { id = result.Id, version = this.GetApiVersion() },
            result
        );
    }

    /// <summary>
    /// Streams the stored file back to the caller, disposing the underlying stream on error to
    /// prevent resource leaks.
    /// </summary>
    [HttpGet("{id:guid}/download")]
    [RequirePermission(Permission.Examples.Download)]
    public async Task<IActionResult> Download(
        [FromRoute] DownloadFileRequest request,
        CancellationToken ct
    )
    {
        var result = await _sender.Send(new DownloadFileQuery(request), ct);
        try
        {
            return File(result.FileStream, result.ContentType, result.FileName);
        }
        catch
        {
            await result.FileStream.DisposeAsync();
            throw;
        }
    }
}
