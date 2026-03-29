using Contracts.IntegrationEvents.Sagas;
using FileStorage.Application.Common.Contracts;
using FileStorage.Application.Common.Options;
using FileStorage.Application.Features.Files.Commands;
using FileStorage.Domain.Interfaces;
using FileStorage.Infrastructure.FileStorage;
using FileStorage.Infrastructure.Persistence;
using FileStorage.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Api.Extensions;
using SharedKernel.Api.OutputCaching;
using SharedKernel.Application.Security;
using SharedKernel.Messaging.Conventions;
using SharedKernel.Messaging.Topology;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSharedSerilog();
builder.Services.AddSharedObservability(builder.Configuration, builder.Environment, "file-storage");

builder.Services.AddValidatedOptions<FileStorageOptions>(
    builder.Configuration,
    FileStorageOptions.SectionName
);

builder.Services.AddDbContext<FileStorageDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetRequiredConnectionString("FileStorageDb"))
);

builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<FileStorageDbContext>());

builder.Services.AddSharedInfrastructure<FileStorageDbContext>(builder.Configuration);

builder.Services.AddScoped<IStoredFileRepository, StoredFileRepository>();
builder.Services.AddSingleton<IRolePermissionMap, DefaultRolePermissionMap>();

builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddSharedKeycloakJwtBearer(builder.Configuration, builder.Environment);
builder.Services.AddSharedAuthorization(enablePermissionPolicies: true);

builder.Services.AddControllers();
builder.Services.AddSharedOpenApiDocumentation();
builder.Services.AddSharedOutputCaching(builder.Configuration);

builder.Services.AddHealthChecks();

builder.Host.UseWolverine(opts =>
{
    opts.ApplySharedConventions();
    opts.ApplySharedRetryPolicies();

    opts.Discovery.IncludeAssembly(typeof(UploadFileCommand).Assembly);
    opts.Discovery.IncludeAssembly(typeof(IFileStorageService).Assembly);
    opts.Discovery.IncludeAssembly(typeof(CacheInvalidationHandler).Assembly);

    opts.PersistMessagesWithPostgresql(
        builder.Configuration.GetRequiredConnectionString("FileStorageDb"),
        "wolverine"
    );
    opts.UseEntityFrameworkCoreTransactions();

    opts.UseSharedRabbitMq(builder.Configuration);

    opts.ListenToRabbitQueue(
        RabbitMqTopology.Queues.FileStorage.ProductDeleted,
        queue =>
        {
            queue.BindExchange(RabbitMqTopology.Exchanges.ProductCatalog);
        }
    );

    // Route product deletion cascade completion back to ProductCatalog saga queue.
    opts.PublishMessage<FilesCascadeCompleted>()
        .ToRabbitQueue(RabbitMqTopology.Queues.ProductCatalog.FilesCascadeCompleted);
});

WebApplication app = builder.Build();

await app.MigrateDbAsync<FileStorageDbContext>();

app.UseSharedMicroserviceApiPipeline(true, a => a.MapControllers());

await app.RunAsync();

public partial class Program;
