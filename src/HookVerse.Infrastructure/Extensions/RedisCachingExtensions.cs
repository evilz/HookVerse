using HookVerse.Infrastructure.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HookVerse.Infrastructure.Extensions;

/// <summary>
/// Extension methods for configuring Redis caching.
/// </summary>
public static class RedisCachingExtensions
{
    /// <summary>
    /// Add Redis distributed caching to the service collection.
    /// </summary>
    public static IServiceCollection AddRedisDistributedCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration.GetSection("RedisCache").Get<RedisCacheOptions>() 
                      ?? new RedisCacheOptions();

        services.Configure<RedisCacheOptions>(configuration.GetSection("RedisCache"));

        if (!options.Enabled)
        {
            // Use in-memory cache as fallback
            services.AddDistributedMemoryCache();
            return services;
        }

        // Add StackExchange.Redis distributed cache
        services.AddStackExchangeRedisCache(redisOptions =>
        {
            redisOptions.Configuration = options.ConnectionString;
            redisOptions.InstanceName = options.InstanceName;

            redisOptions.ConfigurationOptions = new StackExchange.Redis.ConfigurationOptions
            {
                EndPoints = { options.ConnectionString },
                ConnectTimeout = options.ConnectTimeoutMs,
                SyncTimeout = options.SyncTimeoutMs,
                AbortOnConnectFail = options.AbortOnConnectFail,
                ConnectRetry = options.RetryOnTimeout ? 3 : 1
            };
        });

        // Register the Redis cache service
        services.AddSingleton<IRedisCacheService, RedisCacheService>();

        return services;
    }
}
