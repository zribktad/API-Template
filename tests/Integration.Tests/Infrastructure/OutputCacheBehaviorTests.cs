using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FileStorage.Domain.Entities;
using FileStorage.Infrastructure.Persistence;
using Identity.Infrastructure.Persistence;
using Integration.Tests.Factories;
using Integration.Tests.Fixtures;
using Integration.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Reviews.Domain.Entities;
using Reviews.Infrastructure.Persistence;
using Shouldly;
using TestCommon;
using Xunit;

namespace Integration.Tests.Infrastructure;

[Trait("Category", TestConstants.CategoryName)]
[Collection(TestConstants.CollectionName)]
public sealed class OutputCacheBehaviorTests : IAsyncLifetime
{
    private readonly SharedContainers _containers;
    private ProductCatalogServiceFactory _productCatalogFactory = null!;
    private IdentityServiceFactory _identityFactory = null!;
    private ReviewsServiceFactory _reviewsFactory = null!;
    private FileStorageServiceFactory _fileStorageFactory = null!;

    public OutputCacheBehaviorTests(SharedContainers containers)
    {
        _containers = containers;
    }

    public async ValueTask InitializeAsync()
    {
        _productCatalogFactory = new ProductCatalogServiceFactory(_containers);
        _identityFactory = new IdentityServiceFactory(_containers);
        _reviewsFactory = new ReviewsServiceFactory(_containers);
        _fileStorageFactory = new FileStorageServiceFactory(_containers);

        await Task.WhenAll(
            _productCatalogFactory.InitializeAsync().AsTask(),
            _identityFactory.InitializeAsync().AsTask(),
            _reviewsFactory.InitializeAsync().AsTask(),
            _fileStorageFactory.InitializeAsync().AsTask()
        );
    }

    public async ValueTask DisposeAsync()
    {
        await Task.WhenAll(
            _fileStorageFactory.DisposeAsync().AsTask(),
            _reviewsFactory.DisposeAsync().AsTask(),
            _identityFactory.DisposeAsync().AsTask(),
            _productCatalogFactory.DisposeAsync().AsTask()
        );
    }

    [Fact]
    public async Task ProductCatalog_ReadEndpoint_ReturnsAgeHeaderOnSecondRead()
    {
        var ct = TestContext.Current.CancellationToken;
        var tenantId = Guid.NewGuid();
        HttpClient client = _productCatalogFactory.CreateClient();
        IntegrationAuthHelper.AuthenticateAsTenantAdmin(client, tenantId);

        HttpResponseMessage first = await client.GetAsync("/api/v1/products", ct);
        string firstBody = await first.Content.ReadAsStringAsync(ct);
        first.StatusCode.ShouldBe(HttpStatusCode.OK, firstBody);

        HttpResponseMessage second = await client.GetAsync("/api/v1/products", ct);
        string secondBody = await second.Content.ReadAsStringAsync(ct);
        second.StatusCode.ShouldBe(HttpStatusCode.OK, secondBody);
        second.Headers.Age.ShouldNotBeNull();
    }

    [Fact]
    public async Task ProductCatalog_WarmReadThenWrite_ReadShowsFreshDataAfterInvalidation()
    {
        var ct = TestContext.Current.CancellationToken;
        var tenantId = Guid.NewGuid();
        HttpClient client = _productCatalogFactory.CreateClient();
        IntegrationAuthHelper.AuthenticateAsTenantAdmin(client, tenantId);

        HttpResponseMessage warm = await client.GetAsync("/api/v1/products", ct);
        warm.StatusCode.ShouldBe(HttpStatusCode.OK);

        string productName = $"cache-product-{Guid.NewGuid():N}";
        HttpResponseMessage create = await client.PostAsJsonAsync(
            "/api/v1/products",
            new
            {
                Items = new[]
                {
                    new
                    {
                        Name = productName,
                        Description = "created",
                        Price = 11m,
                    },
                },
            },
            ct
        );
        create.StatusCode.ShouldBe(HttpStatusCode.OK, await create.Content.ReadAsStringAsync(ct));

        // Cache invalidation is async (Wolverine OutgoingMessages) — poll until the eviction propagates.
        await AsyncPoll.UntilTrueAsync(
            async () =>
            {
                HttpResponseMessage r = await client.GetAsync("/api/v1/products", ct);
                string b = await r.Content.ReadAsStringAsync(ct);
                return b.Contains(productName, StringComparison.OrdinalIgnoreCase);
            },
            timeout: TimeSpan.FromSeconds(2),
            interval: TimeSpan.FromMilliseconds(50),
            cancellationToken: ct
        );
    }

    [Fact]
    public async Task ProductCatalog_CacheIsIsolatedByTenant()
    {
        var ct = TestContext.Current.CancellationToken;
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        HttpClient clientA = _productCatalogFactory.CreateClient();
        HttpClient clientB = _productCatalogFactory.CreateClient();
        IntegrationAuthHelper.AuthenticateAsTenantAdmin(clientA, tenantA);
        IntegrationAuthHelper.AuthenticateAsTenantAdmin(clientB, tenantB);

        HttpResponseMessage warmA = await clientA.GetAsync("/api/v1/products", ct);
        warmA.StatusCode.ShouldBe(HttpStatusCode.OK);

        string productName = $"tenant-b-product-{Guid.NewGuid():N}";
        HttpResponseMessage createB = await clientB.PostAsJsonAsync(
            "/api/v1/products",
            new
            {
                Items = new[]
                {
                    new
                    {
                        Name = productName,
                        Description = "tenant-b",
                        Price = 22m,
                    },
                },
            },
            ct
        );
        createB.StatusCode.ShouldBe(HttpStatusCode.OK, await createB.Content.ReadAsStringAsync(ct));

        string bodyB = await (
            await clientB.GetAsync("/api/v1/products", ct)
        ).Content.ReadAsStringAsync(ct);
        bodyB.ShouldContain(productName);

        string bodyA = await (
            await clientA.GetAsync("/api/v1/products", ct)
        ).Content.ReadAsStringAsync(ct);
        bodyA.ShouldNotContain(productName);
    }

    [Fact]
    public async Task Identity_ReadEndpoint_ReturnsAgeHeaderOnSecondRead()
    {
        var ct = TestContext.Current.CancellationToken;
        var tenantId = Guid.NewGuid();
        HttpClient client = _identityFactory.CreateClient();
        IntegrationAuthHelper.AuthenticateAsPlatformAdmin(client, tenantId);

        HttpResponseMessage first = await client.GetAsync("/api/v1/tenants", ct);
        first.StatusCode.ShouldBe(HttpStatusCode.OK, await first.Content.ReadAsStringAsync(ct));

        HttpResponseMessage second = await client.GetAsync("/api/v1/tenants", ct);
        second.StatusCode.ShouldBe(HttpStatusCode.OK, await second.Content.ReadAsStringAsync(ct));
        second.Headers.Age.ShouldNotBeNull();
    }

    [Fact]
    public async Task Identity_WarmReadThenCreateUser_ReadShowsFreshDataAfterInvalidation()
    {
        var ct = TestContext.Current.CancellationToken;
        var tenantId = Guid.NewGuid();
        HttpClient client = _identityFactory.CreateClient();
        IntegrationAuthHelper.AuthenticateAsPlatformAdmin(client, tenantId);

        await using (AsyncServiceScope scope = _identityFactory.Services.CreateAsyncScope())
        {
            IdentityDbContext db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            db.Tenants.Add(
                new Identity.Domain.Entities.Tenant
                {
                    Id = tenantId,
                    TenantId = tenantId,
                    Code = $"seed-{Guid.NewGuid():N}".Substring(0, 10),
                    Name = "Seed Tenant",
                    IsActive = true,
                }
            );
            await db.SaveChangesAsync(ct);
        }

        HttpResponseMessage warm = await client.GetAsync("/api/v1/users", ct);
        warm.StatusCode.ShouldBe(HttpStatusCode.OK);

        string username = $"cache-user-{Guid.NewGuid():N}".Substring(0, 16);
        string email = $"{username}@example.com";
        HttpResponseMessage create = await client.PostAsJsonAsync(
            "/api/v1/users",
            new { Username = username, Email = email },
            ct
        );
        create.StatusCode.ShouldBe(
            HttpStatusCode.Created,
            await create.Content.ReadAsStringAsync(ct)
        );

        string createBody = await create.Content.ReadAsStringAsync(ct);
        Guid createdId = JsonDocument.Parse(createBody).RootElement.GetProperty("id").GetGuid();

        string body = await (
            await client.GetAsync($"/api/v1/users/{createdId}", ct)
        ).Content.ReadAsStringAsync(ct);
        body.ShouldContain(username);
    }

    [Fact]
    public async Task Reviews_ReadEndpoint_ReturnsAgeHeaderOnSecondRead()
    {
        var ct = TestContext.Current.CancellationToken;
        var tenantId = Guid.NewGuid();
        HttpClient client = _reviewsFactory.CreateClient();
        IntegrationAuthHelper.AuthenticateAsTenantAdmin(client, tenantId, Guid.NewGuid());

        Guid productId = Guid.NewGuid();
        await using (AsyncServiceScope scope = _reviewsFactory.Services.CreateAsyncScope())
        {
            ReviewsDbContext db = scope.ServiceProvider.GetRequiredService<ReviewsDbContext>();
            db.ProductProjections.Add(
                new ProductProjection
                {
                    ProductId = productId,
                    TenantId = tenantId,
                    Name = "Cache Age Product",
                    IsActive = true,
                }
            );
            await db.SaveChangesAsync(ct);
        }

        HttpResponseMessage first = await client.GetAsync(
            $"/api/v1/productreviews/by-product/{productId}",
            ct
        );
        first.StatusCode.ShouldBe(HttpStatusCode.OK, await first.Content.ReadAsStringAsync(ct));

        HttpResponseMessage second = await client.GetAsync(
            $"/api/v1/productreviews/by-product/{productId}",
            ct
        );
        second.StatusCode.ShouldBe(HttpStatusCode.OK, await second.Content.ReadAsStringAsync(ct));
        second.Headers.Age.ShouldNotBeNull();
    }

    [Fact]
    public async Task Reviews_WarmReadThenCreateReview_ReadShowsFreshDataAfterInvalidation()
    {
        var ct = TestContext.Current.CancellationToken;
        var tenantId = Guid.NewGuid();
        HttpClient client = _reviewsFactory.CreateClient();
        Guid userId = Guid.NewGuid();
        IntegrationAuthHelper.AuthenticateAsTenantAdmin(client, tenantId, userId);

        Guid productId = Guid.NewGuid();
        await using (AsyncServiceScope scope = _reviewsFactory.Services.CreateAsyncScope())
        {
            ReviewsDbContext db = scope.ServiceProvider.GetRequiredService<ReviewsDbContext>();
            db.ProductProjections.Add(
                new ProductProjection
                {
                    ProductId = productId,
                    TenantId = tenantId,
                    Name = "Cache Test Product",
                    IsActive = true,
                }
            );
            await db.SaveChangesAsync(ct);
        }

        HttpResponseMessage warm = await client.GetAsync(
            $"/api/v1/productreviews/by-product/{productId}",
            ct
        );
        warm.StatusCode.ShouldBe(HttpStatusCode.OK);

        HttpResponseMessage create = await client.PostAsJsonAsync(
            "/api/v1/productreviews",
            new
            {
                ProductId = productId,
                Comment = "cache-review",
                Rating = 5,
            },
            ct
        );
        create.StatusCode.ShouldBe(
            HttpStatusCode.Created,
            await create.Content.ReadAsStringAsync(ct)
        );

        string body = await (
            await client.GetAsync($"/api/v1/productreviews/by-product/{productId}", ct)
        ).Content.ReadAsStringAsync(ct);
        body.ShouldContain("cache-review");
    }

    [Fact]
    public async Task Reviews_CacheIsIsolatedByTenant()
    {
        var ct = TestContext.Current.CancellationToken;
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var tenantBProductId = Guid.NewGuid();

        HttpClient clientA = _reviewsFactory.CreateClient();
        HttpClient clientB = _reviewsFactory.CreateClient();
        IntegrationAuthHelper.AuthenticateAsTenantAdmin(clientA, tenantA, userA);
        IntegrationAuthHelper.AuthenticateAsTenantAdmin(clientB, tenantB, userB);

        await using (AsyncServiceScope scope = _reviewsFactory.Services.CreateAsyncScope())
        {
            ReviewsDbContext db = scope.ServiceProvider.GetRequiredService<ReviewsDbContext>();
            db.ProductProjections.Add(
                new ProductProjection
                {
                    ProductId = tenantBProductId,
                    TenantId = tenantB,
                    Name = "Tenant B Product",
                    IsActive = true,
                }
            );
            await db.SaveChangesAsync(ct);
        }

        HttpResponseMessage warmA = await clientA.GetAsync("/api/v1/productreviews", ct);
        warmA.StatusCode.ShouldBe(HttpStatusCode.OK);

        HttpResponseMessage createB = await clientB.PostAsJsonAsync(
            "/api/v1/productreviews",
            new
            {
                ProductId = tenantBProductId,
                Comment = "tenant-b-review",
                Rating = 4,
            },
            ct
        );
        createB.StatusCode.ShouldBe(
            HttpStatusCode.Created,
            await createB.Content.ReadAsStringAsync(ct)
        );

        string bodyB = await (
            await clientB.GetAsync("/api/v1/productreviews", ct)
        ).Content.ReadAsStringAsync(ct);
        bodyB.ShouldContain("tenant-b-review");

        string bodyA = await (
            await clientA.GetAsync("/api/v1/productreviews", ct)
        ).Content.ReadAsStringAsync(ct);
        bodyA.ShouldNotContain("tenant-b-review");
    }

    [Fact]
    public async Task FileStorage_DownloadEndpoint_ReturnsAgeHeaderOnSecondRead()
    {
        var ct = TestContext.Current.CancellationToken;
        var tenantId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        HttpClient client = _fileStorageFactory.CreateClient();
        IntegrationAuthHelper.AuthenticateAsTenantAdmin(client, tenantId);

        string tenantDir = Path.Combine(Path.GetTempPath(), tenantId.ToString());
        Directory.CreateDirectory(tenantDir);
        string storagePath = Path.Combine(tenantDir, $"{Guid.NewGuid():N}.txt");
        await File.WriteAllTextAsync(storagePath, "cached-file-content", ct);

        await using (AsyncServiceScope scope = _fileStorageFactory.Services.CreateAsyncScope())
        {
            FileStorageDbContext db =
                scope.ServiceProvider.GetRequiredService<FileStorageDbContext>();
            db.StoredFiles.Add(
                new StoredFile
                {
                    Id = fileId,
                    OriginalFileName = "cache.txt",
                    StoragePath = storagePath,
                    ContentType = "text/plain",
                    SizeBytes = 19,
                    Description = "cache-file",
                    TenantId = tenantId,
                }
            );
            await db.SaveChangesAsync(ct);
        }

        HttpResponseMessage first = await client.GetAsync($"/api/v1/files/{fileId}/download", ct);
        string firstBody = await first.Content.ReadAsStringAsync(ct);
        first.StatusCode.ShouldBe(HttpStatusCode.OK, firstBody);

        HttpResponseMessage second = await client.GetAsync($"/api/v1/files/{fileId}/download", ct);
        string secondBody = await second.Content.ReadAsStringAsync(ct);
        second.StatusCode.ShouldBe(HttpStatusCode.OK, secondBody);
        second.Headers.Age.ShouldNotBeNull();

        try
        {
            Directory.Delete(tenantDir, recursive: true);
        }
        catch
        {
            // Best-effort cleanup; temp files are harmless if locked.
        }
    }
}
