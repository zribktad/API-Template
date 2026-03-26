using Asp.Versioning;
using Contracts.IntegrationEvents.Reviews;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Reviews.Application.EventHandlers;
using Reviews.Domain.Interfaces;
using Reviews.Infrastructure.Persistence;
using Reviews.Infrastructure.Repositories;
using SharedKernel.Api.Security;
using SharedKernel.Application.Context;
using SharedKernel.Application.Options;
using SharedKernel.Application.Security;
using SharedKernel.Domain.Interfaces;
using SharedKernel.Infrastructure.Persistence.Auditing;
using SharedKernel.Infrastructure.Persistence.SoftDelete;
using SharedKernel.Infrastructure.Persistence.UnitOfWork;
using SharedKernel.Messaging.Conventions;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.FluentValidation;
using Wolverine.RabbitMQ;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// ──────────────────────────────────────────────────
// EF Core
// ──────────────────────────────────────────────────
builder.Services.AddDbContext<ReviewsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ReviewsDb"))
);

// Register DbContext as the base type for event handlers that receive DbContext
builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<ReviewsDbContext>());

// ──────────────────────────────────────────────────
// Infrastructure services
// ──────────────────────────────────────────────────
builder.Services.Configure<TransactionDefaultsOptions>(
    builder.Configuration.GetSection("TransactionDefaults")
);
builder.Services.AddScoped<IDbTransactionProvider, EfCoreTransactionProvider>();
builder.Services.AddScoped<IUnitOfWork>(sp => new UnitOfWork(
    sp.GetRequiredService<ReviewsDbContext>(),
    sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TransactionDefaultsOptions>>(),
    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<UnitOfWork>>(),
    sp.GetRequiredService<IDbTransactionProvider>()
));
builder.Services.AddScoped<IAuditableEntityStateManager, AuditableEntityStateManager>();
builder.Services.AddScoped<ISoftDeleteProcessor, SoftDeleteProcessor>();

// Repositories
builder.Services.AddScoped<IProductReviewRepository, ProductReviewRepository>();

// Context providers (HTTP-based, registered as scoped)
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, HttpTenantProvider>();
builder.Services.AddScoped<IActorProvider, HttpActorProvider>();
builder.Services.AddSingleton(TimeProvider.System);

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
// FluentValidation
// ──────────────────────────────────────────────────
builder.Services.AddValidatorsFromAssemblyContaining<ProductCreatedEventHandler>();

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

    opts.Discovery.IncludeAssembly(typeof(ProductCreatedEventHandler).Assembly);

    opts.UseEntityFrameworkCoreTransactions();

    string rabbitConnectionString =
        builder.Configuration.GetConnectionString("RabbitMQ")
        ?? "amqp://guest:guest@localhost:5672";

    opts.UseRabbitMq(new Uri(rabbitConnectionString))
        .AutoProvision()
        .EnableWolverineControlQueues();

    opts.PublishMessage<ReviewCreatedIntegrationEvent>()
        .ToRabbitExchange(
            "reviews.events",
            exchange =>
            {
                exchange.ExchangeType = Wolverine.RabbitMQ.ExchangeType.Fanout;
                exchange.IsDurable = true;
            }
        );

    // Listen to integration event queues
    opts.ListenToRabbitQueue(
        "reviews.product-created",
        queue =>
        {
            queue.BindExchange("product-catalog.events");
        }
    );
    opts.ListenToRabbitQueue(
        "reviews.product-deleted",
        queue =>
        {
            queue.BindExchange("product-catalog.events");
        }
    );
    opts.ListenToRabbitQueue(
        "reviews.tenant-deactivated",
        queue =>
        {
            queue.BindExchange("identity.events");
        }
    );
});

// ══════════════════════════════════════════════════
// Build & Configure Pipeline
// ══════════════════════════════════════════════════
WebApplication app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
