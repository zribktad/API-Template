using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Api.Authorization;

namespace SharedKernel.Api.Extensions;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddSharedAuthorization(
        this IServiceCollection services,
        IEnumerable<string>? authenticationSchemes = null,
        bool enablePermissionPolicies = false
    )
    {
        string[] schemes =
            authenticationSchemes?.Where(scheme => !string.IsNullOrWhiteSpace(scheme)).ToArray()
            ?? [JwtBearerDefaults.AuthenticationScheme];

        services.Configure<PermissionAuthorizationOptions>(options =>
        {
            options.AuthenticationSchemes = schemes;
        });

        AuthorizationPolicyBuilder fallbackPolicyBuilder =
            schemes.Length > 0
                ? new AuthorizationPolicyBuilder(schemes)
                : new AuthorizationPolicyBuilder();

        services
            .AddAuthorizationBuilder()
            .SetFallbackPolicy(fallbackPolicyBuilder.RequireAuthenticatedUser().Build());

        if (enablePermissionPolicies)
        {
            services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
            services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        }

        return services;
    }
}
