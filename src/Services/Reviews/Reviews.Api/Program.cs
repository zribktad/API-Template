using Contracts.IntegrationEvents.Reviews;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Reviews.Application.EventHandlers;
using Reviews.Domain.Interfaces;
using Reviews.Infrastructure.Persistence;
using Reviews.Infrastructure.Repositories;
using SharedKernel.Api.Extensions;
using SharedKernel.Application.Security;
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
// Shared infrastructure (UnitOfWork, auditing, tenancy, versioning)
// ──────────────────────────────────────────────────
builder.Services.AddSharedInfrastructure<ReviewsDbContext>(builder.Configuration);

// Repositories
builder.Services.AddScoped<IProductReviewRepository, ProductReviewRepository>();

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

    opts.UseSharedRabbitMq(builder.Configuration);

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
