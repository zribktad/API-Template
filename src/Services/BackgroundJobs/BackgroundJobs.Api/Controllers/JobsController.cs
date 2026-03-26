using BackgroundJobs.Application.Features.Jobs.Commands;
using BackgroundJobs.Application.Features.Jobs.DTOs;
using BackgroundJobs.Application.Features.Jobs.Queries;
using ErrorOr;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Api.ErrorOrMapping;
using Wolverine;

namespace BackgroundJobs.Api.Controllers;

/// <summary>
/// Presentation-layer controller for long-running job submission and
/// asynchronous status polling using a channel-based job queue.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class JobsController(IMessageBus bus) : ControllerBase
{
    /// <summary>
    /// Enqueues a new job and returns 202 Accepted with a Location header pointing to the
    /// status endpoint so the caller can poll for completion.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Submit(SubmitJobRequest request, CancellationToken ct)
    {
        ErrorOr<JobStatusResponse> result = await bus.InvokeAsync<ErrorOr<JobStatusResponse>>(
            new SubmitJobCommand(request),
            ct
        );
        if (result.IsError)
            return result.ToErrorResult(this);

        return AcceptedAtAction(nameof(GetStatus), new { id = result.Value.Id }, result.Value);
    }

    /// <summary>Returns the current execution status of a previously submitted job, or 404 if not found.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JobStatusResponse>> GetStatus(
        [FromRoute] Guid id,
        CancellationToken ct
    )
    {
        ErrorOr<JobStatusResponse> result = await bus.InvokeAsync<ErrorOr<JobStatusResponse>>(
            new GetJobStatusQuery(new GetJobStatusRequest(id)),
            ct
        );
        return result.ToActionResult(this);
    }
}
