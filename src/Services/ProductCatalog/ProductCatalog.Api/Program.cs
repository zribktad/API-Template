using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Polly;
using ProductCatalog.Application.Features.Product.Repositories;
using ProductCatalog.Application.Features.Product.Validation;
using ProductCatalog.Application.Sagas;
using ProductCatalog.Domain.Interfaces;
using ProductCatalog.Infrastructure.Persistence;
using ProductCatalog.Infrastructure.Repositories;
using ProductCatalog.Infrastructure.StoredProcedures;
using SharedKernel.Api.Extensions;
using SharedKernel.Messaging.Conventions;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.FluentValidation;
using Wolverine.RabbitMQ;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSharedSerilog();
builder.Services.AddSharedObservability(
    builder.Configuration,
    builder.Environment,
    "product-catalog"
);

// ──────────────────────────────────────────────────
// EF Core
// ──────────────────────────────────────────────────
builder.Services.AddDbContext<ProductCatalogDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ProductCatalogDb"))
);

builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<ProductCatalogDbContext>());

// ──────────────────────────────────────────────────
// MongoDB
// ──────────────────────────────────────────────────
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDB"));
builder.Services.AddSingleton<MongoDbContext>();

// ──────────────────────────────────────────────────
// Shared infrastructure (UnitOfWork, auditing, tenancy, versioning)
// ──────────────────────────────────────────────────
builder.Services.AddSharedInfrastructure<ProductCatalogDbContext>(builder.Configuration);

// Repositories
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IProductDataRepository, ProductDataRepository>();
builder.Services.AddScoped<IProductDataLinkRepository, ProductDataLinkRepository>();
builder.Services.AddScoped<IStoredProcedureExecutor, StoredProcedureExecutor>();

// Resilience
builder.Services.AddResiliencePipeline(
    ProductCatalog.Application.Common.Resilience.ResiliencePipelineKeys.MongoProductDataDelete,
    pipelineBuilder =>
    {
        pipelineBuilder.AddRetry(
            new Polly.Retry.RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(200),
                BackoffType = Polly.DelayBackoffType.Exponential,
            }
        );
    }
);

// ──────────────────────────────────────────────────
// Authentication
// ──────────────────────────────────────────────────
builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        IConfigurationSection keycloak = builder.Configuration.GetSection("Keycloak");
        string authServerUrl = keycloak["auth-server-url"] ?? "http://localhost:8080";
        string realm = keycloak["realm"] ?? "api-template";
        string resource = keycloak["resource"] ?? "api-template-backend";

        options.Authority = $"{authServerUrl.TrimEnd('/')}/realms/{realm}";
        options.Audience = resource;
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    });

builder.Services.AddAuthorization();

// ──────────────────────────────────────────────────
// FluentValidation
// ──────────────────────────────────────────────────
builder.Services.AddValidatorsFromAssemblyContaining<CreateProductRequestValidator>();

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

    opts.UseFluentValidation();

    opts.Discovery.IncludeAssembly(typeof(ProductDeletionSaga).Assembly);

    opts.UseEntityFrameworkCoreTransactions();

    opts.UseSharedRabbitMq(builder.Configuration);

    // Publish integration events to the product-catalog exchange
    opts.PublishMessage<Contracts.IntegrationEvents.ProductCatalog.ProductCreatedIntegrationEvent>()
        .ToRabbitExchange(
            "product-catalog.events",
            exchange => exchange.ExchangeType = Wolverine.RabbitMQ.ExchangeType.Fanout
        );
    opts.PublishMessage<Contracts.IntegrationEvents.ProductCatalog.ProductDeletedIntegrationEvent>()
        .ToRabbitExchange(
            "product-catalog.events",
            exchange => exchange.ExchangeType = Wolverine.RabbitMQ.ExchangeType.Fanout
        );

    // Listen for saga completion messages
    opts.ListenToRabbitQueue("product-catalog.reviews-cascade-completed");
    opts.ListenToRabbitQueue("product-catalog.files-cascade-completed");
    opts.ListenToRabbitQueue("product-catalog.start-product-deletion-saga");
});

// ══════════════════════════════════════════════════
// Build & Configure Pipeline
// ══════════════════════════════════════════════════
WebApplication app = builder.Build();

using (AsyncServiceScope scope = app.Services.CreateAsyncScope())
{
    ProductCatalogDbContext dbContext =
        scope.ServiceProvider.GetRequiredService<ProductCatalogDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();
