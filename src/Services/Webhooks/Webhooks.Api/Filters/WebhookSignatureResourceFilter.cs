using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Webhooks.Application.Common.Constants;
using Webhooks.Application.Common.Contracts;

namespace Webhooks.Api.Filters;

/// <summary>
/// Resource filter that validates inbound webhook HMAC signatures before the action executes.
/// Enables request body buffering so the raw payload can be read for signature verification
/// and again by the controller action.
/// </summary>
public sealed class WebhookSignatureResourceFilter : IAsyncResourceFilter
{
    private const string InboundSecretConfigKey = "Webhooks:InboundSecret";

    private readonly IWebhookPayloadValidator _validator;
    private readonly IConfiguration _configuration;

    public WebhookSignatureResourceFilter(
        IWebhookPayloadValidator validator,
        IConfiguration configuration
    )
    {
        _validator = validator;
        _configuration = configuration;
    }

    public async Task OnResourceExecutionAsync(
        ResourceExecutingContext context,
        ResourceExecutionDelegate next
    )
    {
        HttpRequest request = context.HttpContext.Request;

        request.EnableBuffering();

        using StreamReader reader = new(request.Body, leaveOpen: true);
        string body = await reader.ReadToEndAsync();
        request.Body.Position = 0;

        string? signature = request.Headers[WebhookConstants.SignatureHeader];
        string? timestamp = request.Headers[WebhookConstants.TimestampHeader];
        string? secret = _configuration[InboundSecretConfigKey];

        if (
            string.IsNullOrEmpty(signature)
            || string.IsNullOrEmpty(timestamp)
            || string.IsNullOrEmpty(secret)
            || !_validator.Validate(body, signature, timestamp, secret)
        )
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        await next();
    }
}
