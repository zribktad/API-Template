using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharedKernel.Api.Options;

namespace SharedKernel.Api.Extensions;

public static class KeycloakReadinessExtensions
{
    public static async Task WaitForKeycloakAsync(this WebApplication app)
    {
        KeycloakReadinessOptions options =
            app.Configuration.GetRequiredSection(KeycloakReadinessOptions.SectionName)
                .Get<KeycloakReadinessOptions>()
            ?? throw new InvalidOperationException("Keycloak configuration section is missing.");

        if (options.SkipReadinessCheck)
            return;

        string discoveryUrl = BuildDiscoveryUrl(options);
        ILogger logger = app
            .Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(KeycloakReadinessExtensions));

        using HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(5) };

        for (int attempt = 1; attempt <= options.ReadinessMaxRetries; attempt++)
        {
            try
            {
                HttpResponseMessage response = await httpClient.GetAsync(discoveryUrl);
                if (response.IsSuccessStatusCode)
                {
                    logger.LogInformation("Keycloak is ready at {DiscoveryUrl}", discoveryUrl);
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // Keycloak not ready yet
            }
            catch (TaskCanceledException)
            {
                // Timeout
            }

            int delayMs = Math.Min(1000 * (int)Math.Pow(2, attempt - 1), 10_000);
            logger.LogWarning(
                "Keycloak not ready at {Url}, attempt {Attempt}/{MaxRetries}. Retrying in {DelayMs}ms",
                discoveryUrl,
                attempt,
                options.ReadinessMaxRetries,
                delayMs
            );

            await Task.Delay(delayMs);
        }

        throw new InvalidOperationException(
            $"Keycloak at {discoveryUrl} did not become ready after {options.ReadinessMaxRetries} attempts."
        );
    }

    private static string BuildDiscoveryUrl(KeycloakReadinessOptions options) =>
        $"{options.AuthServerUrl.TrimEnd('/')}/realms/{options.Realm}/.well-known/openid-configuration";
}
