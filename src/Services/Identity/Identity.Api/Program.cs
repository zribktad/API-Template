using Contracts.IntegrationEvents.Identity;
using Contracts.IntegrationEvents.Sagas;
using FluentValidation;
using Identity.Api.Extensions;
using Identity.Application.Options;
using Identity.Application.Security;
using Identity.Domain.Interfaces;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Repositories;
using Identity.Infrastructure.Security;
using Identity.Infrastructure.Security.Keycloak;
using Identity.Infrastructure.Security.Tenant;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
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
builder.Services.AddSharedObservability(builder.Configuration, builder.Environment, "identity");

builder.Services.AddValidatedOptions<KeycloakOptions>(
    builder.Configuration,
    KeycloakOptions.SectionName
);
builder.Services.AddValidatedOptions<BffOptions>(builder.Configuration, BffOptions.SectionName);
builder.Services.AddValidatedOptions<BootstrapTenantOptions>(
    builder.Configuration,
    BootstrapTenantOptions.SectionName
);
builder.Services.AddValidatedOptions<SystemIdentityOptions>(
    builder.Configuration,
    SystemIdentityOptions.SectionName
);
builder.Services.AddValidatedOptions<InvitationOptions>(
    builder.Configuration,
    InvitationOptions.SectionName
);

builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetRequiredConnectionString("IdentityDb"))
);

builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<IdentityDbContext>());
builder.Services.AddSharedInfrastructure<IdentityDbContext>(builder.Configuration);

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITenantRepository, TenantRepository>();
builder.Services.AddScoped<ITenantInvitationRepository, TenantInvitationRepository>();

builder.Services.AddSingleton<IRolePermissionMap, DefaultRolePermissionMap>();
builder.Services.AddScoped<IKeycloakAdminService, KeycloakAdminService>();
builder.Services.AddScoped<ISecureTokenGenerator, SecureTokenGenerator>();
builder.Services.AddScoped<IUserProvisioningService, UserProvisioningService>();
builder.Services.AddSingleton<KeycloakAdminTokenProvider>();
builder.Services.AddTransient<KeycloakAdminTokenHandler>();

builder
    .Services.AddSharedKeycloakJwtBearer(
        builder.Configuration,
        builder.Environment,
        requireTenantClaim: true,
        options =>
        {
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = TenantClaimValidator.OnTokenValidated,
            };
        }
    )
    .AddIdentityBffAuthentication(builder.Configuration, builder.Environment);

builder.Services.AddSharedAuthorization(
    [JwtBearerDefaults.AuthenticationScheme, AuthConstants.BffSchemes.Cookie],
    enablePermissionPolicies: true
);

builder.Services.AddValidatorsFromAssemblyContaining<IKeycloakAdminService>();

builder.Services.AddControllers();

builder.Services.AddHealthChecks();

builder.Host.UseWolverine(opts =>
{
    opts.ApplySharedConventions();
    opts.ApplySharedRetryPolicies();

    opts.UseFluentValidation();

    opts.Discovery.IncludeAssembly(typeof(IKeycloakAdminService).Assembly);

    opts.UseEntityFrameworkCoreTransactions();

    opts.UseSharedRabbitMq(builder.Configuration);

    opts.PublishMessage<UserRegisteredIntegrationEvent>()
        .ToRabbitExchange(
            RabbitMqTopology.Exchanges.Identity,
            exchange =>
            {
                exchange.ExchangeType = Wolverine.RabbitMQ.ExchangeType.Fanout;
                exchange.IsDurable = true;
            }
        );
    opts.PublishMessage<UserRoleChangedIntegrationEvent>()
        .ToRabbitExchange(RabbitMqTopology.Exchanges.Identity);
    opts.PublishMessage<TenantInvitationCreatedIntegrationEvent>()
        .ToRabbitExchange(RabbitMqTopology.Exchanges.Identity);
    opts.PublishMessage<TenantDeactivatedIntegrationEvent>()
        .ToRabbitExchange(RabbitMqTopology.Exchanges.Identity);

    // Handle the TenantDeactivated event for user cascade
    opts.ListenToRabbitQueue(
        RabbitMqTopology.Queues.Identity.TenantDeactivated,
        queue =>
        {
            queue.BindExchange(RabbitMqTopology.Exchanges.Identity);
        }
    );

    // Listen for saga completion messages routed back to this service
    opts.ListenToRabbitQueue(RabbitMqTopology.Queues.Identity.UsersCascadeCompleted);
    opts.ListenToRabbitQueue(RabbitMqTopology.Queues.Identity.ProductsCascadeCompleted);
    opts.ListenToRabbitQueue(RabbitMqTopology.Queues.Identity.CategoriesCascadeCompleted);

    // Route completion messages to their designated queues so the saga receives them
    opts.PublishMessage<UsersCascadeCompleted>()
        .ToRabbitQueue(RabbitMqTopology.Queues.Identity.UsersCascadeCompleted);
});

WebApplication app = builder.Build();

await app.MigrateDbAsync<IdentityDbContext>();

app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();

await app.RunAsync();
