using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCaching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace HookVerse.Api.Middleware;

/// <summary>
/// Configuration options for response caching
/// </summary>
public class ResponseCachingOptions
{
    /// <summary>
    /// Whether response caching is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Default cache duration in seconds (default: 5 minutes)
    /// </summary>
    public int DefaultDurationSeconds { get; set; } = 300;

    /// <summary>
    /// Maximum cache size in MB (default: 100 MB)
    /// </summary>
    public int MaxCacheSizeMB { get; set; } = 100;

    /// <summary>
    /// Whether to vary by query string
    /// </summary>
    public bool VaryByQueryString { get; set; } = true;

    /// <summary>
    /// Whether to vary by user
    /// </summary>
    public bool VaryByUser { get; set; } = false;

    /// <summary>
    /// Paths to exclude from caching (regex patterns)
    /// </summary>
    public List<string> ExcludedPaths { get; set; } = new()
    {
        "/api/v1/webhooks$",  // POST webhooks (mutations)
        "/api/v1/events$",    // POST events (mutations)
        "/health",            // Health checks
        "/metrics"            // Metrics endpoints
    };
}

/// <summary>
/// Attribute to control response caching on controllers/actions
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ResponseCacheAttribute : Attribute
{
    /// <summary>
    /// Cache duration in seconds
    /// </summary>
    public int Duration { get; set; }

    /// <summary>
    /// Whether to vary by query string
    /// </summary>
    public bool VaryByQueryString { get; set; } = true;

    /// <summary>
    /// Specific query string parameters to vary by
    /// </summary>
    public string[]? VaryByQueryKeys { get; set; }

    /// <summary>
    /// Whether caching is enabled for this endpoint
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Cache location (Client, Server, Any, None)
    /// </summary>
    public string Location { get; set; } = "Any";
}

/// <summary>
/// Extension methods for configuring response caching
/// </summary>
public static class ResponseCachingExtensions
{
    /// <summary>
    /// Adds response caching services
    /// </summary>
    public static IServiceCollection AddHookVerseResponseCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ResponseCachingOptions>(
            configuration.GetSection("ResponseCaching"));

        var options = configuration.GetSection("ResponseCaching").Get<ResponseCachingOptions>() 
            ?? new ResponseCachingOptions();

        if (options.Enabled)
        {
            // Add response caching services
            services.AddResponseCaching(cachingOptions =>
            {
                cachingOptions.MaximumBodySize = options.MaxCacheSizeMB * 1024 * 1024;
                cachingOptions.UseCaseSensitivePaths = false;
            });

            // Add memory cache for application-level caching
            services.AddMemoryCache(memoryCacheOptions =>
            {
                memoryCacheOptions.SizeLimit = options.MaxCacheSizeMB * 1024 * 1024;
            });
        }

        return services;
    }

    /// <summary>
    /// Adds response caching middleware to pipeline
    /// </summary>
    public static IApplicationBuilder UseHookVerseResponseCaching(
        this IApplicationBuilder app)
    {
        var options = app.ApplicationServices
            .GetService<IOptions<ResponseCachingOptions>>()?.Value;

        if (options?.Enabled == true)
        {
            app.UseResponseCaching();
        }

        return app;
    }
}

/// <summary>
/// Controller base class with caching helper methods
/// </summary>
public abstract class CachedControllerBase : ControllerBase
{
    protected readonly IMemoryCache _cache;
    protected readonly ILogger _logger;

    protected CachedControllerBase(IMemoryCache cache, ILogger logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Get or create cached value
    /// </summary>
    protected async Task<T> GetOrCreateCachedAsync<T>(
        string cacheKey,
        Func<Task<T>> factory,
        TimeSpan? duration = null)
    {
        if (_cache.TryGetValue(cacheKey, out T? cachedValue) && cachedValue != null)
        {
            _logger.LogDebug("Cache hit for key: {CacheKey}", cacheKey);
            return cachedValue;
        }

        _logger.LogDebug("Cache miss for key: {CacheKey}", cacheKey);
        
        var value = await factory();
        
        var cacheEntryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = duration ?? TimeSpan.FromMinutes(5),
            Size = 1 // Simple size tracking
        };

        _cache.Set(cacheKey, value, cacheEntryOptions);
        
        return value;
    }

    /// <summary>
    /// Invalidate cache by key
    /// </summary>
    protected void InvalidateCache(string cacheKey)
    {
        _cache.Remove(cacheKey);
        _logger.LogDebug("Invalidated cache for key: {CacheKey}", cacheKey);
    }

    /// <summary>
    /// Invalidate cache by pattern
    /// </summary>
    protected void InvalidateCacheByPattern(string pattern)
    {
        // Note: IMemoryCache doesn't support pattern-based invalidation
        // For pattern-based invalidation, consider using Redis or a custom cache implementation
        _logger.LogWarning(
            "Pattern-based cache invalidation not supported with IMemoryCache. Pattern: {Pattern}",
            pattern);
    }

    /// <summary>
    /// Set response cache headers
    /// </summary>
    protected void SetCacheHeaders(int durationSeconds, bool isPublic = true)
    {
        Response.Headers["Cache-Control"] = isPublic 
            ? $"public, max-age={durationSeconds}"
            : $"private, max-age={durationSeconds}";
        
        Response.Headers["Expires"] = DateTime.UtcNow
            .AddSeconds(durationSeconds)
            .ToString("R");
    }

    /// <summary>
    /// Disable caching for this response
    /// </summary>
    protected void DisableCaching()
    {
        Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["Expires"] = "0";
    }
}
