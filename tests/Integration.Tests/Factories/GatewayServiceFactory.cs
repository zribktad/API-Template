extern alias GatewayApi;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Integration.Tests.Factories;

public sealed class GatewayServiceFactory : WebApplicationFactory<GatewayApi::Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Dictionary<string, string?> config = new()
        {
            ["Keycloak:realm"] = "api-template",
            ["Keycloak:auth-server-url"] = "http://localhost:8180",
            ["ReverseProxy:Clusters:identity:Destinations:destination1:Address"] =
                "http://localhost:5991",
            ["ReverseProxy:Clusters:product-catalog:Destinations:destination1:Address"] =
                "http://localhost:5992",
            ["ReverseProxy:Clusters:reviews:Destinations:destination1:Address"] =
                "http://localhost:5993",
            ["ReverseProxy:Clusters:notifications:Destinations:destination1:Address"] =
                "http://localhost:5994",
            ["ReverseProxy:Clusters:file-storage:Destinations:destination1:Address"] =
                "http://localhost:5995",
            ["ReverseProxy:Clusters:background-jobs:Destinations:destination1:Address"] =
                "http://localhost:5996",
            ["ReverseProxy:Clusters:webhooks:Destinations:destination1:Address"] =
                "http://localhost:5997",
        };

        builder.ConfigureAppConfiguration(
            (_, configurationBuilder) => configurationBuilder.AddInMemoryCollection(config)
        );
        builder.UseEnvironment("Development");
    }
}
