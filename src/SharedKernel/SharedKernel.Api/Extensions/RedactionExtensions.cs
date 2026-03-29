using System.Security.Cryptography;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Application.Logging;

namespace SharedKernel.Api.Extensions;

public static class RedactionExtensions
{
    public static IServiceCollection AddSharedRedaction(this IServiceCollection services)
    {
        services.AddRedaction(builder =>
        {
            builder.SetHmacRedactor(
                options =>
                {
                    options.Key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
                    options.KeyId = 1;
                },
                LogDataClassifications.Sensitive
            );

            builder.SetFallbackRedactor<ErasingRedactor>();
        });

        return services;
    }
}
