using Webhooks.Application.Features.Subscriptions.DTOs;
using Webhooks.Domain.Interfaces;
using Wolverine.Http;

namespace Webhooks.Application.Features.Subscriptions.Endpoints;

/// <summary>
/// Wolverine HTTP endpoint that returns all webhook subscriptions.
/// </summary>
public static class GetWebhookSubscriptionsEndpoint
{
    [WolverineGet("/api/v1/webhooks/subscriptions")]
    public static async Task<IReadOnlyList<WebhookSubscriptionResponse>> HandleAsync(
        IWebhookSubscriptionRepository repository,
        CancellationToken ct
    )
    {
        var subscriptions = await repository.GetAllAsync(ct);

        return subscriptions
            .Select(s => new WebhookSubscriptionResponse(
                s.Id,
                s.Url,
                s.IsActive,
                s.EventTypes.Select(et => et.EventType).ToList(),
                s.Audit.CreatedAtUtc
            ))
            .ToList();
    }
}
