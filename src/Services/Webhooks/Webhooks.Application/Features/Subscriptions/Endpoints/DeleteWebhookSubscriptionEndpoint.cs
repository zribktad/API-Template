using SharedKernel.Domain.Exceptions;
using Webhooks.Domain.Entities;
using Webhooks.Domain.Interfaces;
using Wolverine.Http;

namespace Webhooks.Application.Features.Subscriptions.Endpoints;

/// <summary>
/// Wolverine HTTP endpoint that deletes a webhook subscription by its identifier.
/// </summary>
public static class DeleteWebhookSubscriptionEndpoint
{
    [WolverineDelete("/api/v1/webhooks/subscriptions/{id}")]
    public static async Task HandleAsync(
        Guid id,
        IWebhookSubscriptionRepository repository,
        CancellationToken ct
    )
    {
        WebhookSubscription subscription =
            await repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(WebhookSubscription), id);

        await repository.DeleteAsync(subscription, ct);
        await repository.SaveChangesAsync(ct);
    }
}
