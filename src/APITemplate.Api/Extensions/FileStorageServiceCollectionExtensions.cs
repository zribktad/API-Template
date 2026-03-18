using APITemplate.Application.Common.Contracts;
using APITemplate.Application.Common.Options;
using APITemplate.Infrastructure.FileStorage;

namespace APITemplate.Extensions;

public static class FileStorageServiceCollectionExtensions
{
    public static IServiceCollection AddFileStorageServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<FileStorageOptions>(configuration.SectionFor<FileStorageOptions>());
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        return services;
    }
}
