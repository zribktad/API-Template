using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace APITemplate.Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    // Pre-warm the server before parallel test classes start calling CreateClient().
    // Without this, concurrent constructors race to call StartServer(), causing duplicate
    // InMemory DB seed operations ("An item with the same key has already been added").
    public ValueTask InitializeAsync()
    {
        _ = Server;
        return ValueTask.CompletedTask;
    }

    // DisposeAsync() is satisfied by WebApplicationFactory<T>'s IAsyncDisposable implementation.

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        IntegrationTestWebHost.Configure(builder, _dbName);
}
