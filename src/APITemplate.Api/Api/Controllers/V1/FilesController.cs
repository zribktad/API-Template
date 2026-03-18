using APITemplate.Api.Authorization;
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
        IFormFile file,
        [FromForm] string? description,
        CancellationToken ct
    )
    {
        await using var stream = file.OpenReadStream();
        var result = await _sender.Send(
            new UploadFileCommand(
                stream,
                file.FileName,
                file.ContentType,
                file.Length,
                description
            ),
            ct
        );
        return CreatedAtAction(nameof(Download), new { id = result.Id, version = "1.0" }, result);
    }

    [HttpGet("{id:guid}/download")]
    [RequirePermission(Permission.Examples.Download)]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new DownloadFileQuery(id), ct);
        if (result is null)
            return NotFound();
        return File(result.FileStream, result.ContentType, result.FileName);
    }
}
