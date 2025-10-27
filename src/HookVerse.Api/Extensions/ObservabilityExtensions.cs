using HookVerse.Infrastructure.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace HookVerse.Api.Extensions;

/// <summary>
/// Extension methods for configuring observability
/// </summary>
public static class ObservabilityExtensions
{
    /// <summary>
    /// Configure Serilog logging
    /// </summary>
    public static void AddSerilogLogging(this WebApplicationBuilder builder)
    {
        var logConfig = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "HookVerse.Api")
            .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName);

        // In development, always write to console for Aspire Dashboard visibility
        if (builder.Environment.IsDevelopment())
        {
            logConfig.WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");
        }
        else
        {
            // In production, write to console with structured format for log aggregation
            logConfig.WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");
        }

        // Always write to file for persistence
        logConfig.WriteTo.File(
            "logs/hookverse-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");

        Log.Logger = logConfig.CreateLogger();

        builder.Host.UseSerilog();
    }

    /// <summary>
    /// Add health checks for dependencies
    /// </summary>
    public static IServiceCollection AddAdvancedHealthChecks(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var healthChecks = services.AddHealthChecks();

        // Database health check
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrEmpty(connectionString))
        {
            healthChecks.AddNpgSql(
                connectionString,
                name: "postgres",
                tags: new[] { "database", "postgres" });
        }

        // RabbitMQ health check
        var rabbitMqHost = configuration["RabbitMQ:Host"];
        var rabbitMqUsername = configuration["RabbitMQ:Username"];
        var rabbitMqPassword = configuration["RabbitMQ:Password"];
        
        if (!string.IsNullOrEmpty(rabbitMqHost))
        {
            healthChecks.AddRabbitMQ(
                sp =>
                {
                    var factory = new RabbitMQ.Client.ConnectionFactory
                    {
                        HostName = rabbitMqHost,
                        UserName = rabbitMqUsername ?? "guest",
                        Password = rabbitMqPassword ?? "guest",
                        Port = 5672
                    };
                    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
                },
                name: "rabbitmq",
                tags: new[] { "messagebus", "rabbitmq" });
        }

        // Azure Key Vault health check (Production/Staging only)
        if (environment.IsProduction() || environment.IsStaging())
        {
            healthChecks.AddCheck<KeyVaultHealthCheck>(
                name: "keyvault",
                tags: new[] { "azure", "keyvault", "security" });
        }

        return services;
    }
}
