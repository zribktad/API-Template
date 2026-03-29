using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace APITemplate.Tests.Integration;

public sealed class AlbaRateLimitingFixture : AlbaApiFixture
{
    public const int PermitLimit = 3;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
            services.Configure<RateLimitingOptions>(o =>
            {
                o.PermitLimit = PermitLimit;
                o.WindowMinutes = 1;
            })
        );
    }
}
