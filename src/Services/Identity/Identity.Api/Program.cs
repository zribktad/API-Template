using Asp.Versioning;
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
// Infrastructure services
// ──────────────────────────────────────────────────
builder.Services.Configure<TransactionDefaultsOptions>(
    builder.Configuration.GetSection("TransactionDefaults")
);
builder.Services.AddScoped<IDbTransactionProvider, EfCoreTransactionProvider>();
builder.Services.AddScoped<IUnitOfWork>(sp => new UnitOfWork(
    sp.GetRequiredService<IdentityDbContext>(),
    sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TransactionDefaultsOptions>>(),
    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<UnitOfWork>>(),
    sp.GetRequiredService<IDbTransactionProvider>()
));
builder.Services.AddScoped<IAuditableEntityStateManager, AuditableEntityStateManager>();
builder.Services.AddScoped<ISoftDeleteProcessor, SoftDeleteProcessor>();

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
builder.Services.AddSingleton(TimeProvider.System);

// Context providers (HTTP-based, registered as scoped)
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, HttpTenantProvider>();
builder.Services.AddScoped<IActorProvider, HttpActorProvider>();

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

    string rabbitConnectionString =
        builder.Configuration.GetConnectionString("RabbitMQ")
        ?? "amqp://guest:guest@localhost:5672";

    opts.UseRabbitMq(new Uri(rabbitConnectionString))
        .AutoProvision()
        .EnableWolverineControlQueues();
});

// ══════════════════════════════════════════════════
// Build & Configure Pipeline
// ══════════════════════════════════════════════════
WebApplication app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
