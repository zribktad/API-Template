using Alba;
using Microsoft.AspNetCore.Hosting;
using Xunit;

namespace APITemplate.Tests.Integration;

public class AlbaApiFixture : IAsyncLifetime
{
    private readonly string _dbName = Guid.NewGuid().ToString();
    private IAlbaHost? _host;

    public IAlbaHost Host =>
        _host ?? throw new InvalidOperationException("Fixture not initialized.");

    protected virtual void ConfigureWebHost(IWebHostBuilder builder) =>
        IntegrationTestWebHost.Configure(builder, _dbName);

    public async ValueTask InitializeAsync()
    {
        _host = await AlbaHost.For<Program>(ConfigureWebHost);
    }

    public async ValueTask DisposeAsync()
    {
        if (_host is not null)
            await _host.DisposeAsync();
    }
}
