using Microsoft.Extensions.Configuration;

namespace SharedKernel.Api.Extensions;

public static class ConfigurationExtensions
{
    public static string GetRequiredConnectionString(
        this IConfiguration configuration,
        string connectionStringName
    ) =>
        configuration.GetConnectionString(connectionStringName)
        ?? throw new InvalidOperationException(
            $"Connection string '{connectionStringName}' is not configured."
        );

    public static TOptions GetRequiredOptions<TOptions>(
        this IConfiguration configuration,
        string sectionName
    )
        where TOptions : class =>
        configuration.GetRequiredSection(sectionName).Get<TOptions>()
        ?? throw new InvalidOperationException(
            $"Configuration section '{sectionName}' is invalid or missing required values."
        );
}
