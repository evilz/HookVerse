using Microsoft.Extensions.Configuration;

namespace HookVerse.Infrastructure.Configuration;

/// <summary>
/// Connection string provider for production environments using Azure managed services.
/// Retrieves connection strings for Azure SQL Database, Azure Service Bus, and Azure Cache for Redis.
/// Connection strings are expected to be configured via Azure App Configuration or Key Vault references.
/// </summary>
public class AzureConnectionStringProvider : IConnectionStringProvider
{
    private readonly IConfiguration _configuration;

    public AzureConnectionStringProvider(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Gets the Azure SQL Database connection string from configuration.
    /// Expected connection string name: "AzureSqlDatabase" or fallback to "postgres" for compatibility.
    /// </summary>
    public string GetPostgresConnectionString()
    {
        // Try Azure-specific connection string first
        var connectionString = _configuration.GetConnectionString("AzureSqlDatabase");
        
        // Fallback to generic "postgres" name for backward compatibility
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = _configuration.GetConnectionString("postgres");
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Azure SQL Database connection string not found. " +
                "Configure 'AzureSqlDatabase' or 'postgres' connection string in Azure App Configuration or Key Vault. " +
                "Expected format: Server=tcp:<server>.database.windows.net,1433;Database=hookverse;User ID=<user>;Password=<password>;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;");
        }

        return connectionString;
    }

    /// <summary>
    /// Gets the Azure Service Bus connection string from configuration.
    /// Expected connection string name: "AzureServiceBus" or fallback to "rabbitmq" for compatibility.
    /// </summary>
    public string GetRabbitMqConnectionString()
    {
        // Try Azure-specific connection string first
        var connectionString = _configuration.GetConnectionString("AzureServiceBus");
        
        // Fallback to generic "rabbitmq" name for backward compatibility
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = _configuration.GetConnectionString("rabbitmq");
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Azure Service Bus connection string not found. " +
                "Configure 'AzureServiceBus' or 'rabbitmq' connection string in Azure App Configuration or Key Vault. " +
                "Expected format: Endpoint=sb://<namespace>.servicebus.windows.net/;SharedAccessKeyName=<keyname>;SharedAccessKey=<key>");
        }

        return connectionString;
    }

    /// <summary>
    /// Gets the Azure Cache for Redis connection string from configuration.
    /// Expected connection string name: "AzureRedisCache" or fallback to "redis" for compatibility.
    /// </summary>
    public string GetRedisConnectionString()
    {
        // Try Azure-specific connection string first
        var connectionString = _configuration.GetConnectionString("AzureRedisCache");
        
        // Fallback to generic "redis" name for backward compatibility
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = _configuration.GetConnectionString("redis");
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Azure Redis Cache connection string not found. " +
                "Configure 'AzureRedisCache' or 'redis' connection string in Azure App Configuration or Key Vault. " +
                "Expected format: <cachename>.redis.cache.windows.net:6380,password=<accesskey>,ssl=True,abortConnect=False");
        }

        return connectionString;
    }
}
