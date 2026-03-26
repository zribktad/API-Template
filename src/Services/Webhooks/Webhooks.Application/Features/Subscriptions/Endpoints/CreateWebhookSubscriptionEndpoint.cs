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
    public static async Task<WebhookSubscriptionResponse> HandleAsync(
        CreateWebhookSubscriptionRequest request,
        IWebhookSubscriptionRepository repository,
        CancellationToken ct
    )
    {
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

        return new WebhookSubscriptionResponse(
            subscription.Id,
            subscription.Url,
            subscription.IsActive,
            request.EventTypes.ToList(),
            subscription.Audit.CreatedAtUtc
        );
    }
}
