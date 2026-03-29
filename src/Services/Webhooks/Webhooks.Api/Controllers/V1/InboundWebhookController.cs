using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Api.Controllers;
using Webhooks.Api.Filters;
using Webhooks.Application.Common.Constants;
using Webhooks.Application.Common.Contracts;
using Webhooks.Application.Common.DTOs;

namespace Webhooks.Api.Controllers.V1;

[ApiVersion(1.0)]
[AllowAnonymous]
[RequestSizeLimit(1_048_576)]
[ServiceFilter(typeof(WebhookSignatureResourceFilter))]
/// <summary>
/// Accepts inbound webhook callbacks, validates their HMAC signature via the resource filter,
/// and enqueues them for asynchronous background processing.
/// </summary>
public sealed class InboundWebhookController(IWebhookInboundQueue queue) : ApiControllerBase
{
    /// <summary>Receives an inbound webhook event and enqueues it for processing.</summary>
    [HttpPost]
    public async Task<IActionResult> Receive(
        [FromHeader(Name = WebhookConstants.EventTypeHeader)] string eventType,
        CancellationToken ct
    )
    {
        using StreamReader reader = new(Request.Body);
        string payload = await reader.ReadToEndAsync(ct);

        InboundWebhookMessage message = new(eventType, payload, TimeProvider.System.GetUtcNow());
        await queue.EnqueueAsync(message, ct);

        return Accepted();
    }
}
