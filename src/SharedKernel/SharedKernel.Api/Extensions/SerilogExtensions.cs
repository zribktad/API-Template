using Microsoft.Extensions.Hosting;
using Serilog;
using SharedKernel.Infrastructure.Logging;

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
                    .Enrich.With<ActivityTraceEnricher>()
                    .Enrich.WithProperty("Application", context.HostingEnvironment.ApplicationName);
            }
        );
    }
}
