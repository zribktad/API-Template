using Webhooks.Domain.Entities;
using Webhooks.Domain.Interfaces;
using Webhooks.Infrastructure.Persistence;

namespace Webhooks.Infrastructure.Repositories;

/// <summary>
/// EF Core repository for <see cref="WebhookDeliveryLog"/> that provides persistence
/// against the <see cref="WebhooksDbContext"/>.
/// </summary>
public sealed class WebhookDeliveryLogRepository : IWebhookDeliveryLogRepository
{
    private readonly WebhooksDbContext _dbContext;

    public WebhookDeliveryLogRepository(WebhooksDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task AddAsync(WebhookDeliveryLog log, CancellationToken ct = default)
    {
        await _dbContext.WebhookDeliveryLogs.AddAsync(log, ct);
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _dbContext.SaveChangesAsync(ct);
    }
}
