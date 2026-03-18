using APITemplate.Application.Features.Examples.DTOs;
using APITemplate.Application.Features.Examples.Handlers;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APITemplate.Api.Controllers.V1;

[ApiVersion(1.0)]
[ApiController]
[Route("api/v{version:apiVersion}/examples/webhooks")]
public sealed class WebhooksController : ControllerBase
{
    private readonly ISender _sender;

    public WebhooksController(ISender sender) => _sender = sender;

    [HttpPost]
    [AllowAnonymous]
    [RequestSizeLimit(1024 * 1024)] // 1 MB max for webhook payloads
    public async Task<IActionResult> Receive(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync(ct);

        if (
            !Request.Headers.TryGetValue(WebhookConstants.SignatureHeader, out var signature)
            || !Request.Headers.TryGetValue(WebhookConstants.TimestampHeader, out var timestamp)
        )
        {
            return Unauthorized();
        }

        await _sender.Send(
            new ReceiveWebhookCommand(
                new ReceiveWebhookRequest(body, signature.ToString(), timestamp.ToString())
            ),
            ct
        );

        return Ok();
    }
}
