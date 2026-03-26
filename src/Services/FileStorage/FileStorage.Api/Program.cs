using FileStorage.Application.Common.Contracts;
using FileStorage.Application.Common.Options;
using FileStorage.Domain.Interfaces;
using FileStorage.Infrastructure.FileStorage;
using FileStorage.Infrastructure.Persistence;
using FileStorage.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Api.Extensions;
using SharedKernel.Messaging.Conventions;
using SharedKernel.Messaging.Topology;
using Wolverine;
using Wolverine.EntityFrameworkCore;
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

builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();

builder.Services.AddControllers();

builder.Services.AddHealthChecks();

builder.Host.UseWolverine(opts =>
{
    opts.ApplySharedConventions();
    opts.ApplySharedRetryPolicies();

    opts.Discovery.IncludeAssembly(typeof(IFileStorageService).Assembly);

    opts.UseEntityFrameworkCoreTransactions();

    opts.UseSharedRabbitMq(builder.Configuration);

    opts.ListenToRabbitQueue(
        RabbitMqTopology.Queues.FileStorage.ProductDeleted,
        queue =>
        {
            queue.BindExchange(RabbitMqTopology.Exchanges.ProductCatalog);
        }
    );
});

WebApplication app = builder.Build();

await app.MigrateDbAsync<FileStorageDbContext>();

app.MapHealthChecks("/health");
app.MapControllers();

await app.RunAsync();
