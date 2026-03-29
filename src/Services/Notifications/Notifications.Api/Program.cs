using Microsoft.EntityFrameworkCore;
using Notifications.Application.Common.Constants;
using Notifications.Application.Features.Emails.EventHandlers;
using Notifications.Application.Options;
using Notifications.Domain.Interfaces;
using Notifications.Infrastructure.Email;
using Notifications.Infrastructure.Persistence;
using Notifications.Infrastructure.Repositories;
using Polly;
using Polly.Retry;
using SharedKernel.Api.Extensions;
using SharedKernel.Messaging.Conventions;
using SharedKernel.Messaging.Topology;
using Wolverine;
using Wolverine.Http;
using Wolverine.RabbitMQ;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSharedSerilog();
builder.Services.AddSharedObservability(
    builder.Configuration,
    builder.Environment,
    "notifications"
);

// Database
string connectionString = builder.Configuration.GetRequiredConnectionString("DefaultConnection");

builder.Services.AddDbContext<NotificationsDbContext>(options =>
    options.UseNpgsql(connectionString)
);

// Email options
builder.Services.AddValidatedOptions<EmailOptions>(builder.Configuration, EmailOptions.SectionName);

// Email services (queue, sender, renderer, store)
builder.Services.AddSingleton<ChannelEmailQueue>();
builder.Services.AddSingleton<IEmailQueue>(sp => sp.GetRequiredService<ChannelEmailQueue>());
builder.Services.AddSingleton<IEmailQueueReader>(sp => sp.GetRequiredService<ChannelEmailQueue>());
builder.Services.AddSingleton<IEmailTemplateRenderer, FluidEmailTemplateRenderer>();
builder.Services.AddTransient<IEmailSender, MailKitEmailSender>();
builder.Services.AddSingleton<IFailedEmailStore, FailedEmailStore>();
builder.Services.AddHostedService<EmailSendingBackgroundService>();

// TimeProvider for UTC timestamps
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSharedKeycloakJwtBearer(builder.Configuration, builder.Environment);
builder.Services.AddSharedAuthorization();

// Repository
builder.Services.AddScoped<IFailedEmailRepository, FailedEmailRepository>();

// Health checks
builder.Services.AddHealthChecks();
builder.Services.AddSharedOpenApiDocumentation();
builder.Services.AddWolverineHttp();

// Wolverine with RabbitMQ
builder.Host.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof(UserRegisteredNotificationHandler).Assembly);

    // Shared conventions
    opts.ApplySharedConventions();
    opts.ApplySharedRetryPolicies();

    // RabbitMQ transport
    opts.UseSharedRabbitMq(builder.Configuration);

    // Listen to notification queues
    opts.ListenToRabbitQueue(
        RabbitMqTopology.Queues.Notifications.UserRegistered,
        queue =>
        {
            queue.BindExchange(RabbitMqTopology.Exchanges.Identity);
        }
    );
    opts.ListenToRabbitQueue(
        RabbitMqTopology.Queues.Notifications.UserRoleChanged,
        queue =>
        {
            queue.BindExchange(RabbitMqTopology.Exchanges.Identity);
        }
    );
    opts.ListenToRabbitQueue(
        RabbitMqTopology.Queues.Notifications.InvitationCreated,
        queue =>
        {
            queue.BindExchange(RabbitMqTopology.Exchanges.Identity);
        }
    );
});

// Resilience pipeline for SMTP retries
EmailOptions emailOptions = builder.Configuration.GetRequiredOptions<EmailOptions>(
    EmailOptions.SectionName
);

builder.Services.AddResiliencePipeline(
    NotificationConstants.SmtpResiliencePipelineKey,
    pipelineBuilder =>
    {
        pipelineBuilder.AddRetry(
            new RetryStrategyOptions
            {
                MaxRetryAttempts = emailOptions.MaxRetryAttempts,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromSeconds(emailOptions.RetryBaseDelaySeconds),
                UseJitter = true,
            }
        );
    }
);

WebApplication app = builder.Build();

await app.MigrateDbAsync<NotificationsDbContext>();

app.UseAuthentication();
app.UseAuthorization();
app.MapSharedOpenApiEndpoint();
app.MapWolverineEndpoints();
app.MapHealthChecks("/health").AllowAnonymous();

await app.RunAsync();

public partial class Program;
