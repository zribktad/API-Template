using Webhooks.Domain.Entities;

namespace Webhooks.Domain.Interfaces;

/// <summary>
/// Repository abstraction for persisting <see cref="WebhookDeliveryLog"/> records.
/// </summary>
public interface IWebhookDeliveryLogRepository
{
    Task AddAsync(WebhookDeliveryLog log, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
