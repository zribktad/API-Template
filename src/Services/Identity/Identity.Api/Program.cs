using Contracts.IntegrationEvents.Identity;
using FluentValidation;
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
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.FluentValidation;
using Wolverine.RabbitMQ;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSharedSerilog();
builder.Services.AddSharedObservability(builder.Configuration, builder.Environment, "identity");

// ──────────────────────────────────────────────────
// Options
// ──────────────────────────────────────────────────
builder.Services.Configure<KeycloakOptions>(builder.Configuration.GetSection("Keycloak"));
builder.Services.Configure<BffOptions>(builder.Configuration.GetSection("Bff"));
builder.Services.Configure<BootstrapTenantOptions>(
    builder.Configuration.GetSection("BootstrapTenant")
);
builder.Services.Configure<SystemIdentityOptions>(
    builder.Configuration.GetSection("SystemIdentity")
);
builder.Services.Configure<InvitationOptions>(builder.Configuration.GetSection("Invitation"));

// ──────────────────────────────────────────────────
// EF Core
// ──────────────────────────────────────────────────
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("IdentityDb"))
);

// ──────────────────────────────────────────────────
// Shared infrastructure (UnitOfWork, auditing, tenancy, versioning)
// ──────────────────────────────────────────────────
builder.Services.AddSharedInfrastructure<IdentityDbContext>(builder.Configuration);

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITenantRepository, TenantRepository>();
builder.Services.AddScoped<ITenantInvitationRepository, TenantInvitationRepository>();

// Security
builder.Services.AddSingleton<IRolePermissionMap, StaticRolePermissionMap>();
builder.Services.AddScoped<IKeycloakAdminService, KeycloakAdminService>();
builder.Services.AddScoped<ISecureTokenGenerator, SecureTokenGenerator>();
builder.Services.AddScoped<IUserProvisioningService, UserProvisioningService>();
builder.Services.AddSingleton<KeycloakAdminTokenProvider>();
builder.Services.AddTransient<KeycloakAdminTokenHandler>();

// ──────────────────────────────────────────────────
// Authentication
// ──────────────────────────────────────────────────
builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        KeycloakOptions keycloak = builder
            .Configuration.GetSection("Keycloak")
            .Get<KeycloakOptions>()!;
        options.Authority = KeycloakUrlHelper.BuildAuthority(
            keycloak.AuthServerUrl,
            keycloak.Realm
        );
        options.Audience = keycloak.Resource;
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = TenantClaimValidator.OnTokenValidated,
        };
    });

builder.Services.AddAuthorization();

// ──────────────────────────────────────────────────
// FluentValidation
// ──────────────────────────────────────────────────
builder.Services.AddValidatorsFromAssemblyContaining<IKeycloakAdminService>();

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

    opts.Discovery.IncludeAssembly(typeof(IKeycloakAdminService).Assembly);

    opts.UseEntityFrameworkCoreTransactions();

    opts.UseSharedRabbitMq(builder.Configuration);

    opts.PublishMessage<UserRegisteredIntegrationEvent>()
        .ToRabbitExchange(
            "identity.events",
            exchange =>
            {
                exchange.ExchangeType = Wolverine.RabbitMQ.ExchangeType.Fanout;
                exchange.IsDurable = true;
            }
        );
    opts.PublishMessage<UserRoleChangedIntegrationEvent>().ToRabbitExchange("identity.events");
    opts.PublishMessage<TenantInvitationCreatedIntegrationEvent>()
        .ToRabbitExchange("identity.events");
    opts.PublishMessage<TenantDeactivatedIntegrationEvent>().ToRabbitExchange("identity.events");
});

// ══════════════════════════════════════════════════
// Build & Configure Pipeline
// ══════════════════════════════════════════════════
WebApplication app = builder.Build();

using (AsyncServiceScope scope = app.Services.CreateAsyncScope())
{
    IdentityDbContext dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();
