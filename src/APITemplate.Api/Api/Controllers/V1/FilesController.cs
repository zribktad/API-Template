using APITemplate.Api.Authorization;
using APITemplate.Api.Requests;
using APITemplate.Application.Common.Security;
using APITemplate.Application.Features.Examples.DTOs;
using APITemplate.Application.Features.Examples.Handlers;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace APITemplate.Api.Controllers.V1;

[ApiVersion(1.0)]
[ApiController]
[Route("api/v{version:apiVersion}/examples/files")]
public sealed class FilesController : ControllerBase
{
    private readonly ISender _sender;

    public FilesController(ISender sender) => _sender = sender;

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
        return CreatedAtAction(nameof(Download), new { id = result.Id, version = "1.0" }, result);
    }

    [HttpGet("{id:guid}/download")]
    [RequirePermission(Permission.Examples.Download)]
    public async Task<IActionResult> Download(
        [FromRoute] DownloadFileRequest request,
        CancellationToken ct
    )
    {
        var result = await _sender.Send(new DownloadFileQuery(request), ct);
        if (result is null)
            return NotFound();
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
