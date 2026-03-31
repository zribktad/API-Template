using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SharedKernel.Api.OutputCaching;
using SharedKernel.Application.Common.Events;

namespace SharedKernel.Api.Extensions;

public static class OutputCachingExtensions
{
    public static IServiceCollection AddSharedOutputCaching(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        string? dragonflyConnectionString = configuration.GetConnectionString("Dragonfly");

        if (!string.IsNullOrWhiteSpace(dragonflyConnectionString))
        {
            services.AddStackExchangeRedisOutputCache(options =>
            {
                options.Configuration = dragonflyConnectionString;
                options.InstanceName = RedisInstanceNames.OutputCache;
            });
        }
        else
        {
            services.AddOutputCache();
        }

        services.AddScoped<IOutputCacheInvalidationService, OutputCacheInvalidationService>();

        services
            .AddOptions<CachingOptions>()
            .Bind(configuration.GetSection(CachingOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IConfigureOptions<OutputCacheOptions>>(sp =>
        {
            CachingOptions cachingOptions = sp.GetRequiredService<IOptions<CachingOptions>>().Value;

            return new ConfigureOptions<OutputCacheOptions>(options =>
            {
                options.AddBasePolicy(builder => builder.NoCache());

                ReadOnlySpan<(string Name, int ExpirationSeconds)> policies =
                [
                    (CacheTags.Products, cachingOptions.ProductsExpirationSeconds),
                    (CacheTags.Categories, cachingOptions.CategoriesExpirationSeconds),
                    (CacheTags.Reviews, cachingOptions.ReviewsExpirationSeconds),
                    (CacheTags.ProductData, cachingOptions.ProductDataExpirationSeconds),
                    (CacheTags.Tenants, cachingOptions.TenantsExpirationSeconds),
                    (
                        CacheTags.TenantInvitations,
                        cachingOptions.TenantInvitationsExpirationSeconds
                    ),
                    (CacheTags.Users, cachingOptions.UsersExpirationSeconds),
                    (CacheTags.Files, cachingOptions.FilesExpirationSeconds),
                ];

                foreach (var (name, expirationSeconds) in policies)
                {
                    options.AddPolicy(
                        name,
                        new TenantAwareOutputCachePolicy(
                            name,
                            TimeSpan.FromSeconds(expirationSeconds)
                        )
                    );
                }
            });
        });

        return services;
    }

    public static WebApplication UseSharedOutputCaching(this WebApplication app)
    {
        app.UseOutputCache();
        return app;
    }
}
