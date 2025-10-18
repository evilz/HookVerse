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
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "HookVerse.Api")
            .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .WriteTo.File(
                "logs/hookverse-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .CreateLogger();

        builder.Host.UseSerilog();
    }

    /// <summary>
    /// Add health checks for dependencies
    /// </summary>
    public static IServiceCollection AddAdvancedHealthChecks(this IServiceCollection services, IConfiguration configuration)
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

        return services;
    }
}
