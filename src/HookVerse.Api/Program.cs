using HookVerse.Api.Extensions;
using HookVerse.Api.Middleware;
using Serilog;

// Main entry point
var builder = WebApplication.CreateBuilder(args);
ConfigureServices(builder);
var app = builder.Build();
ConfigurePipeline(app);

try
{
    Log.Information("HookVerse API started successfully");
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

return;

// Configure services (used by both main and tests)
static void ConfigureServices(WebApplicationBuilder builder)
{
    // Configure Serilog logging
    builder.AddSerilogLogging();

    Log.Information("Starting HookVerse API");

    // Add services to the container
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    // Add API versioning
    builder.Services.AddApiVersioningConfiguration();

    // Add Swagger/OpenAPI
    builder.Services.AddSwaggerConfiguration();

    // Add CORS
    builder.Services.AddCorsConfiguration();

    // Add database and repositories
    builder.Services.AddDatabase(builder.Configuration);

    // Add message bus
    builder.Services.AddMessageBus(builder.Configuration);

    // Add authentication
    builder.Services.AddApiKeyAuthentication();

    // Add business services
    builder.Services.AddBusinessServices();

    // Add health checks
    builder.Services.AddAdvancedHealthChecks(builder.Configuration);

    // Add exception handling
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();
}

// Configure HTTP pipeline (used by both main and tests)
static void ConfigurePipeline(WebApplication app)
{
    // Configure the HTTP request pipeline
    app.UseExceptionHandler();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "HookVerse API v1");
        });
    }

    app.UseHttpsRedirection();

    // Use CORS
    app.UseCors(app.Environment.IsDevelopment() ? "AllowAll" : "Production");

    // Add mock endpoint middleware (before authentication)
    app.UseMiddleware<MockEndpointMiddleware>();

    // Add API key authentication middleware
    app.UseMiddleware<ApiKeyAuthenticationMiddleware>();

    app.MapControllers();
    app.MapHealthChecks("/health");
}

// Expose Program for WebApplicationFactory in integration tests
public partial class Program { }
