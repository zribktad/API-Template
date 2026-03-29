using System.Threading.RateLimiting;
using Gateway.Api.Options;
using SharedKernel.Api.Extensions;
using SharedKernel.Api.Observability;

namespace Gateway.Api.Extensions;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddGatewayRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        RateLimitingOptions rateLimitingOptions =
            configuration.GetRequiredOptions<RateLimitingOptions>(RateLimitingOptions.SectionName);

        services.AddValidatedOptions<RateLimitingOptions>(
            configuration,
            RateLimitingOptions.SectionName
        );

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, cancellationToken) =>
            {
                ILogger logger = context
                    .HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger(typeof(RateLimitingExtensions));

                string partitionKey = ResolvePartitionKey(context.HttpContext);
                logger.LogWarning("Rate limit exceeded for partition {PartitionKey}", partitionKey);
                ApiMetrics.RecordRateLimitRejection(partitionKey);

                await Task.CompletedTask;
            };

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                httpContext =>
                {
                    string partitionKey = ResolvePartitionKey(httpContext);

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = rateLimitingOptions.PermitLimit,
                            Window = TimeSpan.FromMinutes(rateLimitingOptions.WindowMinutes),
                        }
                    );
                }
            );
        });

        return services;
    }

    private static string ResolvePartitionKey(HttpContext httpContext)
    {
        return httpContext.User?.Identity?.Name
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";
    }
}
