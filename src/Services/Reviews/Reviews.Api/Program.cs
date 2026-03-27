using Contracts.IntegrationEvents.Reviews;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Reviews.Application.EventHandlers;
using Reviews.Domain.Interfaces;
using Reviews.Infrastructure.Persistence;
using Reviews.Infrastructure.Repositories;
using SharedKernel.Api.Extensions;
using SharedKernel.Application.Security;
using SharedKernel.Messaging.Conventions;
using SharedKernel.Messaging.Topology;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.FluentValidation;
using Wolverine.RabbitMQ;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSharedSerilog();
builder.Services.AddSharedObservability(builder.Configuration, builder.Environment, "reviews");

builder.Services.AddDbContext<ReviewsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetRequiredConnectionString("ReviewsDb"))
);

// Register DbContext as the base type for event handlers that receive DbContext
builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<ReviewsDbContext>());

builder.Services.AddSharedInfrastructure<ReviewsDbContext>(builder.Configuration);

builder.Services.AddScoped<IProductReviewRepository, ProductReviewRepository>();
builder.Services.AddSingleton<IRolePermissionMap, DefaultRolePermissionMap>();

builder.Services.AddSharedKeycloakJwtBearer(builder.Configuration, builder.Environment);
builder.Services.AddSharedAuthorization(enablePermissionPolicies: true);

builder.Services.AddValidatorsFromAssemblyContaining<ProductCreatedEventHandler>();

builder.Services.AddControllers();

builder.Services.AddHealthChecks();

builder.Host.UseWolverine(opts =>
{
    opts.ApplySharedConventions();
    opts.ApplySharedRetryPolicies();

    opts.UseFluentValidation();

    opts.Discovery.IncludeAssembly(typeof(ProductCreatedEventHandler).Assembly);

    opts.UseEntityFrameworkCoreTransactions();

    opts.UseSharedRabbitMq(builder.Configuration);

    opts.PublishMessage<ReviewCreatedIntegrationEvent>()
        .ToRabbitExchange(
            RabbitMqTopology.Exchanges.Reviews,
            exchange =>
            {
                exchange.ExchangeType = Wolverine.RabbitMQ.ExchangeType.Fanout;
                exchange.IsDurable = true;
            }
        );

    // Listen to integration event queues
    opts.ListenToRabbitQueue(
        RabbitMqTopology.Queues.Reviews.ProductCreated,
        queue =>
        {
            queue.BindExchange(RabbitMqTopology.Exchanges.ProductCatalog);
        }
    );
    opts.ListenToRabbitQueue(
        RabbitMqTopology.Queues.Reviews.ProductDeleted,
        queue =>
        {
            queue.BindExchange(RabbitMqTopology.Exchanges.ProductCatalog);
        }
    );
    opts.ListenToRabbitQueue(
        RabbitMqTopology.Queues.Reviews.TenantDeactivated,
        queue =>
        {
            queue.BindExchange(RabbitMqTopology.Exchanges.Identity);
        }
    );
});

WebApplication app = builder.Build();

await app.MigrateDbAsync<ReviewsDbContext>();

app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();

await app.RunAsync();
