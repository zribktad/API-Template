using Integration.Tests.Factories;
using Integration.Tests.Fixtures;
using Shouldly;
using Xunit;

namespace Integration.Tests.Infrastructure;

[Trait("Category", TestConstants.StartupSmokeCategoryName)]
[Collection(TestConstants.CollectionName)]
public sealed class ServiceStartupSmokeTests
{
    private readonly SharedContainers _containers;

    public ServiceStartupSmokeTests(SharedContainers containers)
    {
        _containers = containers;
    }

    [Fact]
    public async Task AllServices_Start_And_AreHealthy()
    {
        await using GatewayServiceFactory gatewayFactory = new();
        await AssertHealthyAsync(gatewayFactory.CreateClient());

        await AssertServiceHealthyAsync(new ProductCatalogServiceFactory(_containers));
        await AssertServiceHealthyAsync(new ReviewsServiceFactory(_containers));
        await AssertServiceHealthyAsync(new IdentityServiceFactory(_containers));
        await AssertServiceHealthyAsync(new NotificationsServiceFactory(_containers));
        await AssertServiceHealthyAsync(new FileStorageServiceFactory(_containers));
        await AssertServiceHealthyAsync(new BackgroundJobsServiceFactory(_containers));
        await AssertServiceHealthyAsync(new WebhooksServiceFactory(_containers));
    }

    private static async Task AssertHealthyAsync(HttpClient client)
    {
        HttpResponseMessage response = await client.GetAsync("/health");
        response.IsSuccessStatusCode.ShouldBeTrue();
    }

    private static async Task AssertServiceHealthyAsync<TProgram>(
        ServiceFactoryBase<TProgram> factory
    )
        where TProgram : class
    {
        await factory.InitializeAsync();
        try
        {
            await AssertHealthyAsync(factory.CreateClient());
        }
        finally
        {
            await factory.DisposeAsync();
        }
    }
}
