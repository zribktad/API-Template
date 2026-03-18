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
[Route("api/v{version:apiVersion}/jobs")]
public sealed class JobsController : ControllerBase
{
    private readonly ISender _sender;

    public JobsController(ISender sender) => _sender = sender;

    [HttpPost]
    [RequirePermission(Permission.Examples.Execute)]
    public async Task<IActionResult> Submit(SubmitJobRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new SubmitJobCommand(request), ct);
        return AcceptedAtAction(nameof(GetStatus), new { id = result.Id, version = "1.0" }, result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(Permission.Examples.Read)]
    public async Task<ActionResult<JobStatusResponse>> GetStatus(
        [FromRoute] GetJobStatusRequest request,
        CancellationToken ct
    )
    {
        var result = await _sender.Send(new GetJobStatusQuery(request), ct);
        return result is null ? NotFound() : Ok(result);
    }
}
