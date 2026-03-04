using APITemplate.Application.Common.Context;
using APITemplate.Application.Features.Auth.Services;
using APITemplate.Domain.Entities;
using APITemplate.Domain.Enums;
using APITemplate.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
        var sut = new UserService(dbContext);

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
        var sut = new UserService(dbContext);

        var result = await sut.AuthenticateAsync("ADMIN", "admin");

        result.ShouldNotBeNull();
        result.Username.ShouldBe("admin");
    }

    [Fact]
    public async Task AuthenticateAsync_InvalidCredentials_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        await SeedUserAsync(dbContext, "admin", "admin");
        var sut = new UserService(dbContext);

        var result = await sut.AuthenticateAsync("admin", "wrong");

        result.ShouldBeNull();
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
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.Empty,
            Code = "default",
            Name = "Default Tenant",
            IsActive = true
        };

        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync();

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
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
        var sut = new UserService(dbContext);

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
}
