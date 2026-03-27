using Microsoft.EntityFrameworkCore;
using SharedKernel.Application.Context;
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
    private readonly ITenantProvider _tenantProvider;

    public WebhookSubscriptionRepository(
        WebhooksDbContext dbContext,
        ITenantProvider tenantProvider
    )
    {
        _dbContext = dbContext;
        _tenantProvider = tenantProvider;
    }

    /// <inheritdoc />
    public async Task<WebhookSubscription?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbContext
            .WebhookSubscriptions.Include(s => s.EventTypes)
            .FirstOrDefaultAsync(
                s => s.Id == id && s.TenantId == _tenantProvider.TenantId && !s.IsDeleted,
                ct
            );
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<WebhookSubscription>> GetActiveByEventTypeAsync(
        string eventType,
        Guid tenantId,
        CancellationToken ct = default
    )
    {
        return await _dbContext
            .WebhookSubscriptions.Where(s => s.IsActive && !s.IsDeleted && s.TenantId == tenantId)
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
            .Where(s => !s.IsDeleted && s.TenantId == _tenantProvider.TenantId)
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
