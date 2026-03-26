using Microsoft.AspNetCore.Http;
using Webhooks.Application.Features.Subscriptions.DTOs;
using Webhooks.Domain.Entities;
using Webhooks.Domain.Interfaces;
using Wolverine.Http;

namespace Webhooks.Application.Features.Subscriptions.Endpoints;

/// <summary>
/// Wolverine HTTP endpoint that creates a new webhook subscription.
/// </summary>
public static class CreateWebhookSubscriptionEndpoint
{
    [WolverinePost("/api/v1/webhooks/subscriptions")]
    public static async Task<IResult> HandleAsync(
        CreateWebhookSubscriptionRequest request,
        IWebhookSubscriptionRepository repository,
        CancellationToken ct
    )
    {
        List<string> errors = Validate(request);
        if (errors.Count > 0)
            return Results.ValidationProblem(
                new Dictionary<string, string[]> { ["request"] = errors.ToArray() }
            );

        WebhookSubscription subscription = new()
        {
            Id = Guid.NewGuid(),
            Url = request.Url,
            Secret = request.Secret,
            IsActive = true,
            EventTypes = request
                .EventTypes.Select(et => new WebhookSubscriptionEventType
                {
                    Id = Guid.NewGuid(),
                    EventType = et,
                })
                .ToList(),
        };

        await repository.AddAsync(subscription, ct);
        await repository.SaveChangesAsync(ct);

        WebhookSubscriptionResponse response = new(
            subscription.Id,
            subscription.Url,
            subscription.IsActive,
            request.EventTypes.ToList(),
            subscription.Audit.CreatedAtUtc
        );

        return Results.Created($"/api/v1/webhooks/subscriptions/{subscription.Id}", response);
    }

    private static List<string> Validate(CreateWebhookSubscriptionRequest request)
    {
        List<string> errors = [];

        if (
            !Uri.TryCreate(request.Url, UriKind.Absolute, out Uri? uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
        )
        {
            errors.Add("Url must be a valid absolute HTTP or HTTPS URL.");
        }

        if (
            string.IsNullOrWhiteSpace(request.Secret)
            || request.Secret.Length < WebhookSubscription.SecretMinLength
        )
        {
            errors.Add(
                $"Secret must be at least {WebhookSubscription.SecretMinLength} characters long."
            );
        }

        if (request.EventTypes is null || request.EventTypes.Count == 0)
        {
            errors.Add("EventTypes must contain at least one event type.");
        }

        return errors;
    }
}
