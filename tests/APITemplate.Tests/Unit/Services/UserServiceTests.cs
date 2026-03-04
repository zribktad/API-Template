using APITemplate.Application.Common.Context;
using APITemplate.Application.Common.Options;
using APITemplate.Application.Features.Auth.Services;
using APITemplate.Domain.Entities;
using APITemplate.Domain.Enums;
using APITemplate.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace APITemplate.Tests.Unit.Services;

public class UserServiceTests
{
    [Fact]
    public async Task AuthenticateAsync_CorrectCredentials_ReturnsUser()
    {
        await using var dbContext = CreateDbContext();
        await SeedUserAsync(dbContext, "admin", "admin");
        var sut = CreateSut(dbContext);

        var result = await sut.AuthenticateAsync("admin", "admin");

        result.ShouldNotBeNull();
        result.Username.ShouldBe("admin");
        result.Role.ShouldBe(UserRole.TenantUser);
    }

    [Fact]
    public async Task AuthenticateAsync_UsernameIsCaseInsensitive_ReturnsUser()
    {
        await using var dbContext = CreateDbContext();
        await SeedUserAsync(dbContext, "admin", "admin");
        var sut = CreateSut(dbContext);

        var result = await sut.AuthenticateAsync("ADMIN", "admin");

        result.ShouldNotBeNull();
        result.Username.ShouldBe("admin");
    }

    [Fact]
    public async Task AuthenticateAsync_InvalidCredentials_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        await SeedUserAsync(dbContext, "admin", "admin");
        var sut = CreateSut(dbContext);

        var result = await sut.AuthenticateAsync("admin", "wrong");

        result.ShouldBeNull();
    }

    [Fact]
    public async Task AuthenticateAsync_SameUsernameAcrossTenants_WithoutPrefix_UsesDefaultTenant()
    {
        await using var dbContext = CreateDbContext();
        var tenant1 = await SeedTenantAsync(dbContext, "tenant-a", "Tenant A");
        var tenant2 = await SeedTenantAsync(dbContext, "default", "Default Tenant");

        await SeedUserAsync(dbContext, tenant1.Id, "admin", "first-pass");
        await SeedUserAsync(dbContext, tenant2.Id, "admin", "second-pass");

        var sut = CreateSut(dbContext);
        var result = await sut.AuthenticateAsync("admin", "second-pass");

        result.ShouldNotBeNull();
        result.TenantId.ShouldBe(tenant2.Id);
    }

    [Fact]
    public async Task AuthenticateAsync_SameUsernameAcrossTenants_WithPrefix_UsesSpecifiedTenant()
    {
        await using var dbContext = CreateDbContext();
        var tenant1 = await SeedTenantAsync(dbContext, "tenant-a", "Tenant A");
        var tenant2 = await SeedTenantAsync(dbContext, "default", "Default Tenant");

        await SeedUserAsync(dbContext, tenant1.Id, "admin", "first-pass");
        await SeedUserAsync(dbContext, tenant2.Id, "admin", "second-pass");

        var sut = CreateSut(dbContext);
        var result = await sut.AuthenticateAsync("tenant-a\\admin", "first-pass");

        result.ShouldNotBeNull();
        result.TenantId.ShouldBe(tenant1.Id);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options, new TestTenantProvider(), new TestActorProvider());
    }

    private static async Task SeedUserAsync(
        AppDbContext dbContext,
        string username,
        string password,
        UserRole role = UserRole.TenantUser)
    {
        var tenant = await SeedTenantAsync(dbContext, "default", "Default Tenant");

        await SeedUserAsync(dbContext, tenant.Id, username, password, role);
    }

    private static async Task<Tenant> SeedTenantAsync(AppDbContext dbContext, string code, string name)
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.Empty,
            Code = code,
            Name = name,
            IsActive = true
        };

        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync();
        return tenant;
    }

    private static async Task SeedUserAsync(
        AppDbContext dbContext,
        Guid tenantId,
        string username,
        string password,
        UserRole role = UserRole.TenantUser)
    {
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Username = username,
            Email = "admin@example.com",
            PasswordHash = string.Empty,
            IsActive = true,
            Role = role
        };

        var hasher = new PasswordHasher<AppUser>();
        user.PasswordHash = hasher.HashPassword(user, password);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task AuthenticateAsync_PlatformAdminRole_ReturnsRole()
    {
        await using var dbContext = CreateDbContext();
        await SeedUserAsync(dbContext, "admin", "admin", UserRole.PlatformAdmin);
        var sut = CreateSut(dbContext);

        var result = await sut.AuthenticateAsync("admin", "admin");

        result.ShouldNotBeNull();
        result.Role.ShouldBe(UserRole.PlatformAdmin);
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

    private static UserService CreateSut(AppDbContext dbContext)
    {
        var bootstrapTenantOptions = Options.Create(new BootstrapTenantOptions
        {
            Code = "default",
            Name = "Default Tenant"
        });

        return new UserService(dbContext, bootstrapTenantOptions);
    }
}
