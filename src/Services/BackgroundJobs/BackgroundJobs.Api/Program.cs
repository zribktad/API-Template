using BackgroundJobs.Application.Common;
using BackgroundJobs.Application.Features.Jobs.Commands;
using BackgroundJobs.Application.Options;
using BackgroundJobs.Domain.Interfaces;
using BackgroundJobs.Infrastructure.Persistence;
using BackgroundJobs.Infrastructure.Queue;
using BackgroundJobs.Infrastructure.Repositories;
using BackgroundJobs.Infrastructure.Services;
using BackgroundJobs.Infrastructure.TickerQ;
using BackgroundJobs.Infrastructure.TickerQ.Coordination;
using BackgroundJobs.Infrastructure.TickerQ.Jobs;
using BackgroundJobs.Infrastructure.TickerQ.RecurringJobRegistrations;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Api.Extensions;
using SharedKernel.Messaging.Conventions;
using SharedKernel.Messaging.Topology;
using StackExchange.Redis;
using TickerQ.DependencyInjection;
using TickerQ.EntityFrameworkCore.Customizer;
using TickerQ.EntityFrameworkCore.DependencyInjection;
using TickerQ.Utilities;
using TickerQ.Utilities.Entities;
using Wolverine;
using Wolverine.RabbitMQ;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSharedSerilog();
builder.Services.AddSharedObservability(
    builder.Configuration,
    builder.Environment,
    "background-jobs"
);

// Database
string connectionString = builder.Configuration.GetRequiredConnectionString("DefaultConnection");

builder.Services.AddDbContext<BackgroundJobsDbContext>(options =>
    options.UseNpgsql(connectionString)
);

// Options
builder.Services.AddValidatedOptions<BackgroundJobsOptions>(
    builder.Configuration,
    BackgroundJobsOptions.SectionName
);

BackgroundJobsOptions backgroundJobsOptions =
    builder.Configuration.GetRequiredOptions<BackgroundJobsOptions>(
        BackgroundJobsOptions.SectionName
    );

builder.Services.AddSharedInfrastructure<BackgroundJobsDbContext>(builder.Configuration);

// Repository
builder.Services.AddScoped<IJobExecutionRepository, JobExecutionRepository>();

// Job queue (singleton: both producer and consumer share the same channel)
builder.Services.AddSingleton<ChannelJobQueue>();
builder.Services.AddSingleton<IJobQueue>(sp => sp.GetRequiredService<ChannelJobQueue>());
builder.Services.AddSingleton<IJobQueueReader>(sp => sp.GetRequiredService<ChannelJobQueue>());
builder.Services.AddHostedService<JobProcessingBackgroundService>();

// Services
builder.Services.AddScoped<ICleanupService, CleanupService>();
builder.Services.AddScoped<IReindexService, ReindexService>();

// TickerQ (when enabled)
if (backgroundJobsOptions.TickerQ.Enabled)
{
    string? dragonflyConnectionString = builder.Configuration.GetConnectionString(
        backgroundJobsOptions.TickerQ.CoordinationConnection
    );

    if (!string.IsNullOrWhiteSpace(dragonflyConnectionString))
    {
        builder.Services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(dragonflyConnectionString)
        );
    }

    builder.Services.AddSingleton<IDistributedJobCoordinator, DragonflyDistributedJobCoordinator>();

    string schemaName = TickerQSchedulerOptions.DefaultSchemaName;

    builder.Services.AddDbContext<TickerQSchedulerDbContext>(dbOptions =>
        dbOptions.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", schemaName)
        )
    );

    builder.Services.AddScoped<TickerQRecurringJobRegistrar>();
    builder.Services.AddScoped<
        IRecurringBackgroundJobRegistration,
        CleanupRecurringJobRegistration
    >();
    builder.Services.AddScoped<
        IRecurringBackgroundJobRegistration,
        ReindexRecurringJobRegistration
    >();

    builder.Services.AddTickerQ(tickerOptions =>
    {
        tickerOptions
            .AddOperationalStore(store =>
                store
                    .UseApplicationDbContext<TickerQSchedulerDbContext>(
                        ConfigurationType.IgnoreModelCustomizer
                    )
                    .SetSchema(schemaName)
            )
            .ConfigureScheduler(scheduler =>
            {
                scheduler.NodeIdentifier =
                    $"{backgroundJobsOptions.TickerQ.InstanceNamePrefix}-{Environment.MachineName}-{Environment.ProcessId}";
                scheduler.MaxConcurrency = 1;
            })
            .AddTickerQDiscovery([typeof(CleanupRecurringJob).Assembly]);
    });
}

// Health checks
builder.Services.AddHealthChecks();

// Controllers
builder.Services.AddControllers();

// Wolverine with RabbitMQ
builder.Host.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(
        typeof(BackgroundJobs.Infrastructure.EventHandlers.TenantDeactivatedHandler).Assembly
    );
    opts.Discovery.IncludeAssembly(typeof(SubmitJobCommand).Assembly);

    // Shared conventions
    opts.ApplySharedConventions();
    opts.ApplySharedRetryPolicies();

    // RabbitMQ transport
    opts.UseSharedRabbitMq(builder.Configuration);

    // Listen to background-jobs queues
    opts.ListenToRabbitQueue(
        RabbitMqTopology.Queues.BackgroundJobs.TenantDeactivated,
        queue =>
        {
            queue.BindExchange(RabbitMqTopology.Exchanges.Identity);
        }
    );
});

WebApplication app = builder.Build();

await app.MigrateDbAsync<BackgroundJobsDbContext>();

app.MapControllers();
app.MapHealthChecks("/health");

await app.RunAsync();

public partial class Program;
