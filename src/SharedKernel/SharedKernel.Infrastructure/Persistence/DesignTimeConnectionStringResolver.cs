using Microsoft.Extensions.Configuration;

namespace SharedKernel.Infrastructure.Persistence;

public static class DesignTimeConnectionStringResolver
{
    public static string Resolve(
        string relativeApiProjectPath,
        string connectionStringName,
        string[] args
    )
    {
        string basePath = FindProjectPath(relativeApiProjectPath);
        string? environmentName =
            Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

        return configuration.GetConnectionString(connectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{connectionStringName}' was not found for design-time DbContext creation."
            );
    }

    private static string FindProjectPath(string relativeApiProjectPath)
    {
        DirectoryInfo? current = new(Directory.GetCurrentDirectory());

        while (current is not null)
        {
            string candidate = Path.GetFullPath(
                Path.Combine(current.FullName, relativeApiProjectPath)
            );
            if (Directory.Exists(candidate))
                return candidate;

            current = current.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Unable to locate API project path '{relativeApiProjectPath}' from '{Directory.GetCurrentDirectory()}'."
        );
    }
}
