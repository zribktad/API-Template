using Microsoft.EntityFrameworkCore;
using Webhooks.Domain.Entities;
using Webhooks.Domain.Interfaces;
using Webhooks.Infrastructure.Persistence;

namespace Webhooks.Infrastructure.Repositories;

/// <summary>
/// EF Core repository for <see cref="WebhookSubscription"/> that provides CRUD operations
/// against the <see cref="WebhooksDbContext"/>.
/// </summary>
public sealed class WebhookSubscriptionRepository : IWebhookSubscriptionRepository
{
    private readonly WebhooksDbContext _dbContext;

    public WebhookSubscriptionRepository(WebhooksDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<WebhookSubscription?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbContext
            .WebhookSubscriptions.Include(s => s.EventTypes)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<WebhookSubscription>> GetActiveByEventTypeAsync(
        string eventType,
        CancellationToken ct = default
    )
    {
        return await _dbContext
            .WebhookSubscriptions.Where(s => s.IsActive && !s.IsDeleted)
            .Where(s => s.EventTypes.Any(et => et.EventType == eventType))
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<WebhookSubscription>> GetAllAsync(
        CancellationToken ct = default
    )
    {
        return await _dbContext
            .WebhookSubscriptions.Include(s => s.EventTypes)
            .Where(s => !s.IsDeleted)
            .OrderByDescending(s => s.Audit.CreatedAtUtc)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task AddAsync(WebhookSubscription subscription, CancellationToken ct = default)
    {
        await _dbContext.WebhookSubscriptions.AddAsync(subscription, ct);
    }

    /// <inheritdoc />
    public Task UpdateAsync(WebhookSubscription subscription, CancellationToken ct = default)
    {
        _dbContext.WebhookSubscriptions.Update(subscription);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(WebhookSubscription subscription, CancellationToken ct = default)
    {
        _dbContext.WebhookSubscriptions.Remove(subscription);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _dbContext.SaveChangesAsync(ct);
    }
}
