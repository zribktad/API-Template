WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddHealthChecks();

WebApplication app = builder.Build();

app.MapReverseProxy();
app.MapHealthChecks("/health");

app.Run();
