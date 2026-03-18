using System.Net.Mime;
using System.Text.Json;
using APITemplate.Application.Common.BackgroundJobs;
using APITemplate.Application.Common.Contracts;
using APITemplate.Application.Features.Examples.DTOs;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APITemplate.Api.Controllers.V1;

[ApiVersion(1.0)]
[ApiController]
[Route("api/v{version:apiVersion}/examples/webhooks")]
public sealed class WebhooksController : ControllerBase
{
    private readonly IWebhookPayloadValidator _validator;
    private readonly IWebhookProcessingQueue _queue;

    public WebhooksController(IWebhookPayloadValidator validator, IWebhookProcessingQueue queue)
    {
        _validator = validator;
        _queue = queue;
    }

    [HttpPost]
    [AllowAnonymous]
    [RequestSizeLimit(1024 * 1024)] // 1 MB max for webhook payloads
    public async Task<IActionResult> Receive(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(ct);

        if (
            !Request.Headers.TryGetValue(WebhookConstants.SignatureHeader, out var signature)
            || !Request.Headers.TryGetValue(WebhookConstants.TimestampHeader, out var timestamp)
        )
        {
            return Unauthorized();
        }

        if (!_validator.IsValid(body, signature.ToString(), timestamp.ToString()))
            return Unauthorized();

        WebhookPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<WebhookPayload>(body, JsonSerializerOptions.Web);
        }
        catch (JsonException)
        {
            return BadRequest();
        }

        if (payload is null)
            return BadRequest();

        await _queue.EnqueueAsync(payload, ct);
        return Ok();
    }
}
