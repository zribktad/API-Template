using APITemplate.Api.Authorization;
using APITemplate.Api.Controllers;
using APITemplate.Application.Common.Security;
using APITemplate.Application.Features.Examples.DTOs;
using APITemplate.Application.Features.Examples.Handlers;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace APITemplate.Api.Controllers.V1;

[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/jobs")]
/// <summary>
/// Presentation-layer controller that demonstrates long-running job submission and
/// asynchronous status polling using a channel-based job queue.
/// </summary>
public sealed class JobsController : ApiControllerBase
{
    private readonly ISender _sender;

    public JobsController(ISender sender) => _sender = sender;

    /// <summary>
    /// Enqueues a new job and returns 202 Accepted with a Location header pointing to the
    /// status endpoint so the caller can poll for completion.
    /// </summary>
    [HttpPost]
    [RequirePermission(Permission.Examples.Execute)]
    public async Task<IActionResult> Submit(SubmitJobRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new SubmitJobCommand(request), ct);
        return AcceptedAtAction(
            nameof(GetStatus),
            new { id = result.Id, version = this.GetApiVersion() },
            result
        );
    }

    /// <summary>Returns the current execution status of a previously submitted job, or 404 if not found.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(Permission.Examples.Read)]
    public async Task<ActionResult<JobStatusResponse>> GetStatus(
        [FromRoute] GetJobStatusRequest request,
        CancellationToken ct
    )
    {
        var result = await _sender.Send(new GetJobStatusQuery(request), ct);
        return OkOrNotFound(result);
    }
}
