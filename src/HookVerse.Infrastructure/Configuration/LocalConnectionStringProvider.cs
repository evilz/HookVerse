using Microsoft.Extensions.Configuration;

namespace HookVerse.Infrastructure.Configuration;

/// <summary>
/// Connection string provider for local development using containerized services.
/// Retrieves connection strings from IConfiguration that are injected by Aspire
/// when running via AppHost orchestration.
/// </summary>
public class LocalConnectionStringProvider : IConnectionStringProvider
{
    private readonly IConfiguration _configuration;

    public LocalConnectionStringProvider(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Gets the PostgreSQL connection string from configuration.
    /// Expected to be injected by Aspire AppHost with "postgres" connection name.
    /// </summary>
    public string GetPostgresConnectionString()
    {
        var connectionString = _configuration.GetConnectionString("postgres");
        
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "PostgreSQL connection string 'postgres' not found in configuration. " +
                "Ensure the application is running via Aspire AppHost or connection string is configured in appsettings.json.");
        }

        return connectionString;
    }

    /// <summary>
    /// Gets the RabbitMQ connection string from configuration.
    /// Expected to be injected by Aspire AppHost with "rabbitmq" connection name.
    /// </summary>
    public string GetRabbitMqConnectionString()
    {
        var connectionString = _configuration.GetConnectionString("rabbitmq");
        
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "RabbitMQ connection string 'rabbitmq' not found in configuration. " +
                "Ensure the application is running via Aspire AppHost or connection string is configured in appsettings.json.");
        }

        return connectionString;
    }

    /// <summary>
    /// Gets the Redis connection string from configuration.
    /// Expected to be injected by Aspire AppHost with "redis" connection name.
    /// </summary>
    public string GetRedisConnectionString()
    {
        var connectionString = _configuration.GetConnectionString("redis");
        
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Redis connection string 'redis' not found in configuration. " +
                "Ensure the application is running via Aspire AppHost or connection string is configured in appsettings.json.");
        }

        return connectionString;
    }
}
