namespace HookVerse.Infrastructure.Configuration;

/// <summary>
/// Provides connection strings for infrastructure services in an environment-agnostic way.
/// Implementations should return connection strings appropriate for the current environment
/// (containers for local development, managed services for production).
/// </summary>
public interface IConnectionStringProvider
{
    /// <summary>
    /// Gets the PostgreSQL connection string for the current environment.
    /// </summary>
    /// <returns>A connection string for PostgreSQL database access.</returns>
    string GetPostgresConnectionString();

    /// <summary>
    /// Gets the RabbitMQ connection string for the current environment.
    /// </summary>
    /// <returns>A connection string for RabbitMQ message broker access.</returns>
    string GetRabbitMqConnectionString();

    /// <summary>
    /// Gets the Redis connection string for the current environment.
    /// </summary>
    /// <returns>A connection string for Redis cache access.</returns>
    string GetRedisConnectionString();
}
