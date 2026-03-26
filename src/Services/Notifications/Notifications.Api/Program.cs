using Microsoft.EntityFrameworkCore;
using Notifications.Application.Features.Emails.EventHandlers;
using Notifications.Application.Options;
using Notifications.Domain.Interfaces;
using Notifications.Infrastructure.Email;
using Notifications.Infrastructure.Persistence;
using Notifications.Infrastructure.Repositories;
using Polly;
using Polly.Retry;
using SharedKernel.Messaging.Conventions;
using SharedKernel.Messaging.Topology;
using Wolverine;
using Wolverine.Http;
using Wolverine.RabbitMQ;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Database
string connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection not configured");

builder.Services.AddDbContext<NotificationsDbContext>(options =>
    options.UseNpgsql(connectionString)
);

// Email options
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));

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

// Repository
builder.Services.AddScoped<IFailedEmailRepository, FailedEmailRepository>();

// Health checks
builder.Services.AddHealthChecks();

// Wolverine with RabbitMQ
builder.Host.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof(UserRegisteredNotificationHandler).Assembly);

    // Shared conventions
    opts.ApplySharedConventions();
    opts.ApplySharedRetryPolicies();

    // RabbitMQ transport
    string rabbitHost = builder.Configuration["RabbitMQ:HostName"] ?? "localhost";
    opts.UseRabbitMq(new Uri($"amqp://{rabbitHost}")).AutoProvision();

    // Listen to notification queues
    opts.ListenToRabbitQueue(
        "notifications.user-registered",
        queue =>
        {
            queue.BindExchange("identity.events");
        }
    );
    opts.ListenToRabbitQueue(
        "notifications.user-role-changed",
        queue =>
        {
            queue.BindExchange("identity.events");
        }
    );
    opts.ListenToRabbitQueue(
        "notifications.invitation-created",
        queue =>
        {
            queue.BindExchange("identity.events");
        }
    );
});

// Resilience pipeline for SMTP retries
EmailOptions emailOptions =
    builder.Configuration.GetSection("Email").Get<EmailOptions>() ?? new EmailOptions();

builder.Services.AddResiliencePipeline(
    "smtp-send",
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

app.MapWolverineEndpoints();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
