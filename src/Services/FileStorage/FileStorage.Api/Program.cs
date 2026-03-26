using FileStorage.Application.Common.Contracts;
using FileStorage.Application.Common.Options;
using FileStorage.Domain.Interfaces;
using FileStorage.Infrastructure.FileStorage;
using FileStorage.Infrastructure.Persistence;
using FileStorage.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Api.Extensions;
using SharedKernel.Messaging.Conventions;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.RabbitMQ;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSharedSerilog();
builder.Services.AddSharedObservability(builder.Configuration, builder.Environment, "file-storage");

// ──────────────────────────────────────────────────
// Options
// ──────────────────────────────────────────────────
builder.Services.Configure<FileStorageOptions>(builder.Configuration.GetSection("FileStorage"));

// ──────────────────────────────────────────────────
// EF Core
// ──────────────────────────────────────────────────
builder.Services.AddDbContext<FileStorageDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("FileStorageDb"))
);

// ──────────────────────────────────────────────────
// Shared infrastructure (UnitOfWork, auditing, tenancy, versioning)
// ──────────────────────────────────────────────────
builder.Services.AddSharedInfrastructure<FileStorageDbContext>(builder.Configuration);

// Repositories
builder.Services.AddScoped<IStoredFileRepository, StoredFileRepository>();

// File storage service
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();

// ──────────────────────────────────────────────────
// Controllers
// ──────────────────────────────────────────────────
builder.Services.AddControllers();

// ──────────────────────────────────────────────────
// Wolverine + RabbitMQ
// ──────────────────────────────────────────────────
builder.Host.UseWolverine(opts =>
{
    opts.ApplySharedConventions();

    opts.Discovery.IncludeAssembly(typeof(IFileStorageService).Assembly);

    opts.UseEntityFrameworkCoreTransactions();

    opts.UseSharedRabbitMq(builder.Configuration);

    opts.ListenToRabbitQueue(
        "file-storage.product-deleted",
        queue =>
        {
            queue.BindExchange("product-catalog.events");
        }
    );
});

// ══════════════════════════════════════════════════
// Build & Configure Pipeline
// ══════════════════════════════════════════════════
WebApplication app = builder.Build();

using (AsyncServiceScope scope = app.Services.CreateAsyncScope())
{
    FileStorageDbContext dbContext =
        scope.ServiceProvider.GetRequiredService<FileStorageDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.MapControllers();

await app.RunAsync();
