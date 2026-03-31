using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharedKernel.Application.Options.Security;
using SharedKernel.Infrastructure.Logging;

namespace SharedKernel.Api.Extensions;

public static class LoggingRedactionExtensions
{
    public static IServiceCollection AddSharedLogRedaction(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        IConfigurationSection redactionSection = configuration.GetSection(
            RedactionOptions.SectionName
        );
        if (redactionSection.Exists())
        {
            services.AddValidatedOptions<RedactionOptions>(
                configuration,
                RedactionOptions.SectionName
            );
        }
        else
        {
            services.AddOptions<RedactionOptions>().ValidateDataAnnotations().ValidateOnStart();
        }

        RedactionOptions redactionOptions = GetRedactionOptions(configuration);
        Validator.ValidateObject(
            redactionOptions,
            new ValidationContext(redactionOptions),
            validateAllProperties: true
        );

        string hmacKey = RedactionConfiguration.ResolveHmacKey(
            redactionOptions,
            Environment.GetEnvironmentVariable
        );

        services.AddRedaction(redactionBuilder =>
        {
            redactionBuilder.SetRedactor<ErasingRedactor>(LogDataClassifications.Personal);

#pragma warning disable EXTEXP0002
            redactionBuilder.SetHmacRedactor(
                options =>
                {
                    options.KeyId = redactionOptions.KeyId;
                    options.Key = hmacKey;
                },
                new DataClassificationSet(LogDataClassifications.Sensitive)
            );
#pragma warning restore EXTEXP0002

            redactionBuilder.SetFallbackRedactor<ErasingRedactor>();
        });

        services.AddLogging(logging => logging.EnableRedaction());

        return services;
    }

    internal static RedactionOptions GetRedactionOptions(IConfiguration configuration) =>
        configuration.GetSection(RedactionOptions.SectionName).Get<RedactionOptions>() ?? new();
}
