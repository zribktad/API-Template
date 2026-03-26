using FluentValidation;
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
using SharedKernel.Messaging.Topology;
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

builder.Services.AddDbContext<ProductCatalogDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetRequiredConnectionString("ProductCatalogDb"))
);

builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<ProductCatalogDbContext>());

builder.Services.AddValidatedOptions<MongoDbSettings>(
    builder.Configuration,
    MongoDbSettings.SectionName
);
builder.Services.AddSingleton<MongoDbContext>();

builder.Services.AddSharedInfrastructure<ProductCatalogDbContext>(builder.Configuration);

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IProductDataRepository, ProductDataRepository>();
builder.Services.AddScoped<IProductDataLinkRepository, ProductDataLinkRepository>();
builder.Services.AddScoped<IStoredProcedureExecutor, StoredProcedureExecutor>();

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

builder.Services.AddSharedKeycloakJwtBearer(builder.Configuration, builder.Environment);
builder.Services.AddAuthorization();

builder.Services.AddValidatorsFromAssemblyContaining<CreateProductRequestValidator>();

builder.Services.AddControllers();

builder.Services.AddHealthChecks();

builder.Host.UseWolverine(opts =>
{
    opts.ApplySharedConventions();
    opts.ApplySharedRetryPolicies();

    opts.UseFluentValidation();

    opts.Discovery.IncludeAssembly(typeof(ProductDeletionSaga).Assembly);

    opts.UseEntityFrameworkCoreTransactions();

    opts.UseSharedRabbitMq(builder.Configuration);

    // Publish integration events to the product-catalog exchange
    opts.PublishMessage<Contracts.IntegrationEvents.ProductCatalog.ProductCreatedIntegrationEvent>()
        .ToRabbitExchange(
            RabbitMqTopology.Exchanges.ProductCatalog,
            exchange => exchange.ExchangeType = Wolverine.RabbitMQ.ExchangeType.Fanout
        );
    opts.PublishMessage<Contracts.IntegrationEvents.ProductCatalog.ProductDeletedIntegrationEvent>()
        .ToRabbitExchange(
            RabbitMqTopology.Exchanges.ProductCatalog,
            exchange => exchange.ExchangeType = Wolverine.RabbitMQ.ExchangeType.Fanout
        );

    // Listen for saga completion messages
    opts.ListenToRabbitQueue(RabbitMqTopology.Queues.ProductCatalog.ReviewsCascadeCompleted);
    opts.ListenToRabbitQueue(RabbitMqTopology.Queues.ProductCatalog.FilesCascadeCompleted);
    opts.ListenToRabbitQueue(RabbitMqTopology.Queues.ProductCatalog.StartProductDeletionSaga);
});

WebApplication app = builder.Build();

await app.MigrateDbAsync<ProductCatalogDbContext>();

app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers();

await app.RunAsync();
