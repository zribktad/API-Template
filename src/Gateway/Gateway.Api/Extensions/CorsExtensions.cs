using Gateway.Api.Options;
using SharedKernel.Api.Extensions;

namespace Gateway.Api.Extensions;

public static class CorsExtensions
{
    public const string PolicyName = "GatewayCorsPolicy";

    public static IServiceCollection AddGatewayCors(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        CorsOptions corsOptions = configuration.GetRequiredOptions<CorsOptions>(
            CorsOptions.SectionName
        );

        services.AddValidatedOptions<CorsOptions>(configuration, CorsOptions.SectionName);

        services.AddCors(options =>
        {
            options.AddPolicy(
                PolicyName,
                policy =>
                {
                    policy
                        .WithOrigins(corsOptions.AllowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                }
            );
        });

        return services;
    }

    public static WebApplication UseGatewayCors(this WebApplication app)
    {
        app.UseCors(PolicyName);
        return app;
    }
}
