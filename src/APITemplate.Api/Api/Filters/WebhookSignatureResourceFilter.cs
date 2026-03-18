using APITemplate.Application.Common.Contracts;
using APITemplate.Application.Features.Examples.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace APITemplate.Api.Filters;

public sealed class WebhookSignatureResourceFilter : IAsyncResourceFilter
{
    private readonly IWebhookPayloadValidator _validator;

    public WebhookSignatureResourceFilter(IWebhookPayloadValidator validator)
    {
        _validator = validator;
    }

    public async Task OnResourceExecutionAsync(
        ResourceExecutingContext context,
        ResourceExecutionDelegate next
    )
    {
        var hasAttribute = context.ActionDescriptor.EndpointMetadata.Any(m =>
            m is ValidateWebhookSignatureAttribute
        );

        if (!hasAttribute)
        {
            await next();
            return;
        }

        var request = context.HttpContext.Request;

        if (
            !request.Headers.TryGetValue(WebhookConstants.SignatureHeader, out var signature)
            || !request.Headers.TryGetValue(WebhookConstants.TimestampHeader, out var timestamp)
        )
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        request.EnableBuffering();
        using var reader = new StreamReader(request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync(context.HttpContext.RequestAborted);
        request.Body.Position = 0;

        if (!_validator.IsValid(body, signature.ToString(), timestamp.ToString()))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        await next();
    }
}
