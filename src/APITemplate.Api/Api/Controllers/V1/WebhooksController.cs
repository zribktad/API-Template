using APITemplate.Api.Controllers;
using APITemplate.Api.Filters.Webhooks;
using APITemplate.Application.Common.BackgroundJobs;
using APITemplate.Application.Features.Examples.DTOs;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APITemplate.Api.Controllers.V1;

[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/webhooks")]
public sealed class WebhooksController : ApiControllerBase
{
    private readonly IWebhookProcessingQueue _queue;

    public WebhooksController(IWebhookProcessingQueue queue) => _queue = queue;

    [HttpPost]
    [AllowAnonymous]
    [ValidateWebhookSignature]
    [RequestSizeLimit(1024 * 1024)] // 1 MB max for webhook payloads
    public async Task<IActionResult> Receive(
        [FromBody] WebhookPayload payload,
        CancellationToken ct
    )
    {
        await _queue.EnqueueAsync(payload, ct);
        return Ok();
    }
}
