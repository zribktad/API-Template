using System.Text.Json;
using APITemplate.Application.Common.BackgroundJobs;
using APITemplate.Application.Common.Contracts;
using APITemplate.Application.Common.Errors;
using APITemplate.Application.Features.Examples.DTOs;
using APITemplate.Domain.Exceptions;
using MediatR;

namespace APITemplate.Application.Features.Examples.Handlers;

public sealed record ReceiveWebhookCommand(ReceiveWebhookRequest Request) : IRequest;

public sealed class WebhookRequestHandler : IRequestHandler<ReceiveWebhookCommand>
{
    private readonly IWebhookPayloadValidator _validator;
    private readonly IWebhookProcessingQueue _queue;

    public WebhookRequestHandler(IWebhookPayloadValidator validator, IWebhookProcessingQueue queue)
    {
        _validator = validator;
        _queue = queue;
    }

    public async Task Handle(ReceiveWebhookCommand command, CancellationToken ct)
    {
        var req = command.Request;
        if (!_validator.IsValid(req.Body, req.Signature, req.Timestamp))
            throw new ForbiddenException(
                "Invalid webhook signature.",
                ErrorCatalog.Examples.WebhookInvalidSignature
            );

        WebhookPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<WebhookPayload>(
                req.Body,
                JsonSerializerOptions.Web
            );
        }
        catch (JsonException)
        {
            throw new ValidationException(
                "Invalid webhook payload format.",
                ErrorCatalog.General.ValidationFailed
            );
        }

        if (payload is null)
            throw new ValidationException(
                "Webhook payload cannot be null.",
                ErrorCatalog.General.ValidationFailed
            );

        await _queue.EnqueueAsync(payload, ct);
    }
}
