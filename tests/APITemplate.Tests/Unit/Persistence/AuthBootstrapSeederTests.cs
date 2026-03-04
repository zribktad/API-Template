using APITemplate.Application.Common.Context;
using APITemplate.Application.Common.Options;
using APITemplate.Domain.Entities;
using APITemplate.Domain.Enums;
using APITemplate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace APITemplate.Tests.Unit.Persistence;

public class AuthBootstrapSeederTests
{
    [Fact]
    public async Task SeedAsync_WhenTenantExistsButInactiveOrDeleted_RestoresTenant()
    {
        await using var dbContext = CreateDbContext();
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.Empty,
            Code = "default",
            Name = "Default Tenant",
            IsActive = false,
            IsDeleted = true,
            DeletedAtUtc = DateTime.UtcNow,
            DeletedBy = "test"
        };

        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync();

        var sut = CreateSeeder(dbContext);
        await sut.SeedAsync();

        var restoredTenant = await dbContext.Tenants
            .IgnoreQueryFilters()
            .SingleAsync(t => t.Code == "default");

        restoredTenant.IsActive.ShouldBeTrue();
        restoredTenant.IsDeleted.ShouldBeFalse();
        restoredTenant.DeletedAtUtc.ShouldBeNull();
        restoredTenant.DeletedBy.ShouldBeNull();
    }

    [Fact]
    public async Task SeedAsync_WhenBootstrapUserExistsButInactiveOrDeleted_RestoresUserAndRole()
    {
        await using var dbContext = CreateDbContext();
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.Empty,
            Code = "default",
            Name = "Default Tenant",
            IsActive = true
        };

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Username = "admin",
            NormalizedUsername = "ADMIN",
            Email = "admin@example.com",
            PasswordHash = "existing-hash",
            IsActive = false,
            IsDeleted = true,
            DeletedAtUtc = DateTime.UtcNow,
            DeletedBy = "test",
            Role = UserRole.TenantUser
        };

        dbContext.Tenants.Add(tenant);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var sut = CreateSeeder(dbContext);
        await sut.SeedAsync();

        var restoredUser = await dbContext.Users
            .IgnoreQueryFilters()
            .SingleAsync(u => u.TenantId == tenant.Id && u.NormalizedUsername == "ADMIN");

        restoredUser.IsActive.ShouldBeTrue();
        restoredUser.IsDeleted.ShouldBeFalse();
        restoredUser.DeletedAtUtc.ShouldBeNull();
        restoredUser.DeletedBy.ShouldBeNull();
        restoredUser.Role.ShouldBe(UserRole.PlatformAdmin);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options, new TestTenantProvider(), new TestActorProvider());
    }

    private static AuthBootstrapSeeder CreateSeeder(AppDbContext dbContext)
    {
        var adminOptions = Options.Create(new BootstrapAdminOptions
        {
            Username = "admin",
            Password = "admin",
            Email = "admin@example.com",
            IsPlatformAdmin = true
        });

        var tenantOptions = Options.Create(new BootstrapTenantOptions
        {
            Code = "default",
            Name = "Default Tenant"
        });

        return new AuthBootstrapSeeder(dbContext, adminOptions, tenantOptions);
    }

    private sealed class TestTenantProvider : ITenantProvider
    {
        public Guid TenantId => Guid.Empty;
        public bool HasTenant => false;
    }

    private sealed class TestActorProvider : IActorProvider
    {
        public string ActorId => "test";
    }
}
