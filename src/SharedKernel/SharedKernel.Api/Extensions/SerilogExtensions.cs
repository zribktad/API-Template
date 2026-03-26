using Microsoft.Extensions.Hosting;
using Serilog;

namespace SharedKernel.Api.Extensions;

public static class SerilogExtensions
{
    public static IHostBuilder UseSharedSerilog(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseSerilog(
            (context, services, loggerConfiguration) =>
            {
                loggerConfiguration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("Application", context.HostingEnvironment.ApplicationName);
            }
        );
    }
}
