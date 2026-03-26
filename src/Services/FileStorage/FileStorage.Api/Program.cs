using Asp.Versioning;
using FileStorage.Application.Common.Contracts;
using FileStorage.Application.Common.Options;
using FileStorage.Domain.Interfaces;
using FileStorage.Infrastructure.FileStorage;
using FileStorage.Infrastructure.Persistence;
using FileStorage.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Api.Security;
using SharedKernel.Application.Context;
using SharedKernel.Application.Options;
using SharedKernel.Domain.Interfaces;
using SharedKernel.Infrastructure.Persistence.Auditing;
using SharedKernel.Infrastructure.Persistence.SoftDelete;
using SharedKernel.Infrastructure.Persistence.UnitOfWork;
using SharedKernel.Messaging.Conventions;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.RabbitMQ;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

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
// Infrastructure services
// ──────────────────────────────────────────────────
builder.Services.Configure<TransactionDefaultsOptions>(
    builder.Configuration.GetSection("TransactionDefaults")
);
builder.Services.AddScoped<IDbTransactionProvider, EfCoreTransactionProvider>();
builder.Services.AddScoped<IUnitOfWork>(sp => new UnitOfWork(
    sp.GetRequiredService<FileStorageDbContext>(),
    sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TransactionDefaultsOptions>>(),
    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<UnitOfWork>>(),
    sp.GetRequiredService<IDbTransactionProvider>()
));
builder.Services.AddScoped<IAuditableEntityStateManager, AuditableEntityStateManager>();
builder.Services.AddScoped<ISoftDeleteProcessor, SoftDeleteProcessor>();

// Repositories
builder.Services.AddScoped<IStoredFileRepository, StoredFileRepository>();

// File storage service
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();

// Context providers (HTTP-based, registered as scoped)
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, HttpTenantProvider>();
builder.Services.AddScoped<IActorProvider, HttpActorProvider>();
builder.Services.AddSingleton(TimeProvider.System);

// ──────────────────────────────────────────────────
// API versioning
// ──────────────────────────────────────────────────
builder
    .Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

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

    string rabbitConnectionString =
        builder.Configuration.GetConnectionString("RabbitMQ")
        ?? "amqp://guest:guest@localhost:5672";

    opts.UseRabbitMq(new Uri(rabbitConnectionString))
        .AutoProvision()
        .EnableWolverineControlQueues();

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

app.MapControllers();

app.Run();
