using APITemplate.Api.OpenApi;
using APITemplate.Extensions;
using Microsoft.OpenApi;
using Serilog;

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
var bootstrapConfiguration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(bootstrapConfiguration)
    .Enrich.FromLogContext()
    .CreateLogger();

try
{
    Log.Information("Starting APITemplate");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, loggerConfiguration) =>
    {
        loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext();
    });

    builder.Services.AddControllers();
    builder.Services.AddOpenApi(options =>
    {
        options.AddDocumentTransformer<HealthCheckOpenApiDocumentTransformer>();
        options.AddDocumentTransformer((document, _, _) =>
        {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = " JWT token (without 'Bearer ' prefix)"
            };
            return Task.CompletedTask;
        });
    });
    builder.Services.AddAuthenticationOptions(builder.Configuration);
    builder.Services.AddPersistence(builder.Configuration);
    builder.Services.AddApplicationServices();
    builder.Services.AddMongoDB(builder.Configuration);
    builder.Services.AddJwtAuthentication();
    builder.Services.AddApiVersioningConfiguration();
    builder.Services.AddGraphQLConfiguration();

    var app = builder.Build();

    await app.UseDatabaseAsync();

    app.UseCustomMiddleware();
    app.UseApiDocumentation();
    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapGraphQL();
    app.MapNitroApp("/graphql/ui");
    app.UseHealthChecks();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
