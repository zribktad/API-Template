using Identity.Application.Options;
using Identity.Domain.Entities;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Identity.Infrastructure.Seeding;

public sealed class BootstrapTenantSeeder
{
    private static readonly Guid BootstrapTenantId = Guid.Parse(
        "00000000-0000-0000-0000-000000000001"
    );

    private readonly IdentityDbContext _dbContext;
    private readonly BootstrapTenantOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<BootstrapTenantSeeder> _logger;

    public BootstrapTenantSeeder(
        IdentityDbContext dbContext,
        IOptions<BootstrapTenantOptions> options,
        TimeProvider timeProvider,
        ILogger<BootstrapTenantSeeder> logger
    )
    {
        _dbContext = dbContext;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        Tenant? existing = await _dbContext
            .Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == BootstrapTenantId, cancellationToken);

        if (existing is null)
        {
            DateTime now = _timeProvider.GetUtcNow().UtcDateTime;

            var tenant = new Tenant
            {
                Id = BootstrapTenantId,
                Code = _options.Code,
                Name = _options.Name,
                TenantId = BootstrapTenantId,
                IsActive = true,
                Audit = new SharedKernel.Domain.Entities.AuditInfo
                {
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                },
            };

            _dbContext.Tenants.Add(tenant);
            _logger.LogInformation("Created bootstrap tenant {TenantCode}", _options.Code);
        }
        else
        {
            bool changed = false;

            if (existing.IsDeleted)
            {
                existing.IsDeleted = false;
                existing.DeletedAtUtc = null;
                existing.DeletedBy = null;
                changed = true;
            }

            if (!existing.IsActive)
            {
                existing.IsActive = true;
                changed = true;
            }

            if (changed)
                _logger.LogInformation("Restored bootstrap tenant {TenantCode}", _options.Code);
        }

        if (_dbContext.ChangeTracker.HasChanges())
            await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
