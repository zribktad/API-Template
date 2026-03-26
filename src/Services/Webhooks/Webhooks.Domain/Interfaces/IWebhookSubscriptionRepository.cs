using Webhooks.Domain.Entities;

namespace Webhooks.Domain.Interfaces;

/// <summary>
/// Repository abstraction for <see cref="WebhookSubscription"/> persistence operations.
/// </summary>
public interface IWebhookSubscriptionRepository
{
    Task<WebhookSubscription?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<WebhookSubscription>> GetActiveByEventTypeAsync(
        string eventType,
        CancellationToken ct = default
    );
    Task<IReadOnlyList<WebhookSubscription>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(WebhookSubscription subscription, CancellationToken ct = default);
    Task UpdateAsync(WebhookSubscription subscription, CancellationToken ct = default);
    Task DeleteAsync(WebhookSubscription subscription, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
