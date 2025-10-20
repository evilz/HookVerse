using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace HookVerse.Infrastructure.Extensions;

/// <summary>
/// Extension methods for configuring database connection pooling.
/// </summary>
public static class ConnectionPoolingExtensions
{
    /// <summary>
    /// Add optimized PostgreSQL connection pooling to the service collection.
    /// </summary>
    public static IServiceCollection AddOptimizedPostgreSqlPooling(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration.GetSection("ConnectionPooling").Get<ConnectionPoolingOptions>() 
                      ?? new ConnectionPoolingOptions();

        services.Configure<ConnectionPoolingOptions>(configuration.GetSection("ConnectionPooling"));

        // Build connection string with pooling parameters
        var connectionString = BuildConnectionString(configuration, options);

        // Configure DbContext with optimized settings
        services.AddDbContext<ApplicationDbContext>((serviceProvider, dbOptions) =>
        {
            dbOptions.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: options.MaxRetryCount,
                    maxRetryDelay: TimeSpan.FromSeconds(options.MaxRetryDelaySeconds),
                    errorCodesToAdd: null);

                npgsqlOptions.CommandTimeout(options.CommandTimeoutSeconds);
                npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public");
                
                // Enable connection pooling optimizations
                npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });

            // Enable sensitive data logging in development
            if (configuration.GetValue<bool>("DetailedErrors"))
            {
                dbOptions.EnableSensitiveDataLogging();
                dbOptions.EnableDetailedErrors();
            }

            // Configure query tracking behavior
            dbOptions.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution);
        });

        return services;
    }

    private static string BuildConnectionString(IConfiguration configuration, ConnectionPoolingOptions options)
    {
        var builder = new NpgsqlConnectionStringBuilder(
            configuration.GetConnectionString("DefaultConnection"));

        // Connection pooling settings
        builder.Pooling = options.Pooling;
        builder.MinPoolSize = options.MinPoolSize;
        builder.MaxPoolSize = options.MaxPoolSize;
        builder.ConnectionIdleLifetime = options.ConnectionIdleLifetimeSeconds;
        builder.ConnectionPruningInterval = options.ConnectionPruningIntervalSeconds;

        // Timeout settings
        builder.Timeout = options.ConnectionTimeoutSeconds;
        builder.CommandTimeout = options.CommandTimeoutSeconds;
        builder.KeepAlive = options.KeepAliveSeconds;

        // Performance settings
        builder.NoResetOnClose = options.NoResetOnClose;
        builder.MaxAutoPrepare = options.MaxAutoPrepare;
        builder.AutoPrepareMinUsages = options.AutoPrepareMinUsages;

        // Multiplexing (advanced feature for high concurrency)
        builder.Multiplexing = options.Multiplexing;
        if (options.Multiplexing)
        {
            builder.WriteCoalescingBufferThresholdBytes = options.WriteCoalescingBufferThresholdBytes;
        }

        return builder.ToString();
    }
}

/// <summary>
/// Configuration options for database connection pooling.
/// </summary>
public class ConnectionPoolingOptions
{
    /// <summary>
    /// Enable connection pooling.
    /// </summary>
    public bool Pooling { get; set; } = true;

    /// <summary>
    /// Minimum pool size. Connections below this number are maintained.
    /// </summary>
    public int MinPoolSize { get; set; } = 5;

    /// <summary>
    /// Maximum pool size. Maximum number of connections in the pool.
    /// </summary>
    public int MaxPoolSize { get; set; } = 100;

    /// <summary>
    /// Connection idle lifetime in seconds.
    /// Connections idle longer than this are closed.
    /// </summary>
    public int ConnectionIdleLifetimeSeconds { get; set; } = 300; // 5 minutes

    /// <summary>
    /// Connection pruning interval in seconds.
    /// How often the pool checks for idle connections to remove.
    /// </summary>
    public int ConnectionPruningIntervalSeconds { get; set; } = 10;

    /// <summary>
    /// Connection timeout in seconds.
    /// Time to wait while trying to establish a connection.
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Command timeout in seconds.
    /// Time to wait while executing a command.
    /// </summary>
    public int CommandTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Keep-alive interval in seconds.
    /// Send TCP keep-alive packets to detect dead connections.
    /// </summary>
    public int KeepAliveSeconds { get; set; } = 60;

    /// <summary>
    /// Don't reset connection state when returning to pool.
    /// Improves performance but requires careful transaction management.
    /// </summary>
    public bool NoResetOnClose { get; set; } = false;

    /// <summary>
    /// Maximum number of prepared statements to cache per connection.
    /// Improves performance for frequently executed queries.
    /// </summary>
    public int MaxAutoPrepare { get; set; } = 20;

    /// <summary>
    /// Minimum command usages before auto-prepare.
    /// Commands used this many times are automatically prepared.
    /// </summary>
    public int AutoPrepareMinUsages { get; set; } = 5;

    /// <summary>
    /// Enable connection multiplexing (experimental).
    /// Allows multiple commands on a single physical connection.
    /// </summary>
    public bool Multiplexing { get; set; } = false;

    /// <summary>
    /// Write coalescing buffer threshold in bytes (for multiplexing).
    /// </summary>
    public int WriteCoalescingBufferThresholdBytes { get; set; } = 1000;

    /// <summary>
    /// Maximum retry count for failed operations.
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// Maximum retry delay in seconds.
    /// </summary>
    public int MaxRetryDelaySeconds { get; set; } = 5;

    /// <summary>
    /// Get recommended settings based on environment and load.
    /// </summary>
    public static ConnectionPoolingOptions GetRecommendedSettings(
        string environment,
        LoadProfile loadProfile)
    {
        return (environment.ToLowerInvariant(), loadProfile) switch
        {
            ("development", _) => new ConnectionPoolingOptions
            {
                MinPoolSize = 2,
                MaxPoolSize = 20,
                ConnectionIdleLifetimeSeconds = 600,
                NoResetOnClose = false
            },

            ("staging", LoadProfile.Low) => new ConnectionPoolingOptions
            {
                MinPoolSize = 5,
                MaxPoolSize = 50,
                ConnectionIdleLifetimeSeconds = 300,
                NoResetOnClose = false
            },

            ("production", LoadProfile.Low) => new ConnectionPoolingOptions
            {
                MinPoolSize = 10,
                MaxPoolSize = 100,
                ConnectionIdleLifetimeSeconds = 300,
                NoResetOnClose = false,
                MaxAutoPrepare = 20,
                AutoPrepareMinUsages = 5
            },

            ("production", LoadProfile.Medium) => new ConnectionPoolingOptions
            {
                MinPoolSize = 20,
                MaxPoolSize = 200,
                ConnectionIdleLifetimeSeconds = 300,
                NoResetOnClose = true,
                MaxAutoPrepare = 50,
                AutoPrepareMinUsages = 3
            },

            ("production", LoadProfile.High) => new ConnectionPoolingOptions
            {
                MinPoolSize = 50,
                MaxPoolSize = 500,
                ConnectionIdleLifetimeSeconds = 180,
                NoResetOnClose = true,
                MaxAutoPrepare = 100,
                AutoPrepareMinUsages = 2,
                Multiplexing = true
            },

            _ => new ConnectionPoolingOptions()
        };
    }
}

/// <summary>
/// Load profile for connection pool tuning.
/// </summary>
public enum LoadProfile
{
    /// <summary>
    /// Low load: < 100 requests/second
    /// </summary>
    Low,

    /// <summary>
    /// Medium load: 100-1000 requests/second
    /// </summary>
    Medium,

    /// <summary>
    /// High load: > 1000 requests/second
    /// </summary>
    High
}
