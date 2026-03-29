extern alias ProductCatalogApi;

using Integration.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using ProductCatalog.Domain.Interfaces;
using ProductCatalog.Infrastructure.Persistence;

namespace Integration.Tests.Factories;

public sealed class ProductCatalogServiceFactory : ServiceFactoryBase<ProductCatalogApi::Program>
{
    public ProductCatalogServiceFactory(SharedContainers containers)
        : base(containers) { }

    protected override string ServiceName => "ProductCatalog";
    protected override string ConnectionStringKey => "ProductCatalogDb";

    protected override void ConfigureAdditionalConfiguration(Dictionary<string, string?> config)
    {
        config["MongoDB:ConnectionString"] = "mongodb://localhost:27017";
        config["MongoDB:DatabaseName"] = "test_product_catalog";
    }

    protected override void ConfigureServiceSpecificMocks(IServiceCollection services)
    {
        services.RemoveAll(typeof(MongoDbContext));
        services.RemoveAll(typeof(IProductDataRepository));
        services.AddSingleton(new Mock<IProductDataRepository>().Object);
    }
}
