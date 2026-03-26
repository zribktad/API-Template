using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using SharedKernel.Messaging.Conventions;
using SharedKernel.Messaging.Topology;
using Webhooks.Application.Common.Constants;
using Webhooks.Application.Common.Contracts;
using Webhooks.Application.Features.Delivery.EventHandlers;
using Webhooks.Domain.Interfaces;
using Webhooks.Infrastructure.Delivery;
using Webhooks.Infrastructure.Hmac;
using Webhooks.Infrastructure.Persistence;
using Webhooks.Infrastructure.Repositories;
using Wolverine;
using Wolverine.Http;
using Wolverine.RabbitMQ;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Database
string connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection not configured");

builder.Services.AddDbContext<WebhooksDbContext>(options => options.UseNpgsql(connectionString));

// TimeProvider for HMAC timestamp generation
builder.Services.AddSingleton(TimeProvider.System);

// HMAC signing
builder.Services.AddSingleton<IWebhookPayloadSigner, HmacWebhookPayloadSigner>();

// Webhook delivery
builder.Services.AddScoped<IWebhookDeliveryService, WebhookDeliveryService>();

// Repositories
builder.Services.AddScoped<IWebhookSubscriptionRepository, WebhookSubscriptionRepository>();
builder.Services.AddScoped<IWebhookDeliveryLogRepository, WebhookDeliveryLogRepository>();

// Outgoing HTTP client with retry resilience
builder
    .Services.AddHttpClient(WebhookConstants.OutgoingHttpClientName)
    .AddResilienceHandler(
        "outgoing-webhook-retry",
        pipeline =>
        {
            pipeline.AddRetry(
                new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    BackoffType = DelayBackoffType.Exponential,
                    Delay = TimeSpan.FromSeconds(2),
                    UseJitter = true,
                }
            );
        }
    );

// Health checks
builder.Services.AddHealthChecks();

// Wolverine with RabbitMQ
builder.Host.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof(ProductCreatedWebhookHandler).Assembly);

    // Shared conventions
    opts.ApplySharedConventions();
    opts.ApplySharedRetryPolicies();

    // RabbitMQ transport
    string rabbitHost = builder.Configuration["RabbitMQ:HostName"] ?? "localhost";
    opts.UseRabbitMq(new Uri($"amqp://{rabbitHost}")).AutoProvision();

    // Listen to webhook delivery queues
    opts.ListenToRabbitQueue(
        "webhooks.product-created",
        queue =>
        {
            queue.BindExchange("product-catalog.events");
        }
    );
    opts.ListenToRabbitQueue(
        "webhooks.product-deleted",
        queue =>
        {
            queue.BindExchange("product-catalog.events");
        }
    );
    opts.ListenToRabbitQueue(
        "webhooks.review-created",
        queue =>
        {
            queue.BindExchange("reviews.events");
        }
    );
});

WebApplication app = builder.Build();

app.MapWolverineEndpoints();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
