using HookVerse.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HookVerse.Infrastructure.Caching;

/// <summary>
/// Redis-based distributed cache service for subscription lookups.
/// Implements cache-aside pattern with automatic serialization.
/// </summary>
public class RedisCacheService : IRedisCacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly RedisCacheOptions _options;

    public RedisCacheService(
        IDistributedCache cache,
        ILogger<RedisCacheService> logger,
        Microsoft.Extensions.Options.IOptions<RedisCacheOptions> options)
    {
        _cache = cache;
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// Get cached subscriptions by tenant and event type.
    /// Returns null if not found in cache.
    /// </summary>
    public async Task<List<Subscription>?> GetSubscriptionsAsync(
        Guid tenantId,
        string eventType,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return null;
        }

        var cacheKey = GetSubscriptionCacheKey(tenantId, eventType);

        try
        {
            var cachedBytes = await _cache.GetAsync(cacheKey, cancellationToken);
            if (cachedBytes == null)
            {
                _logger.LogDebug("Cache MISS for key {CacheKey}", cacheKey);
                return null;
            }

            var subscriptions = JsonSerializer.Deserialize<List<Subscription>>(cachedBytes);
            _logger.LogDebug("Cache HIT for key {CacheKey}. Found {Count} subscriptions",
                cacheKey, subscriptions?.Count ?? 0);

            return subscriptions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving from cache for key {CacheKey}", cacheKey);
            // Return null to fallback to database
            return null;
        }
    }

    /// <summary>
    /// Cache subscriptions for a tenant and event type.
    /// </summary>
    public async Task SetSubscriptionsAsync(
        Guid tenantId,
        string eventType,
        List<Subscription> subscriptions,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return;
        }

        var cacheKey = GetSubscriptionCacheKey(tenantId, eventType);

        try
        {
            var json = JsonSerializer.SerializeToUtf8Bytes(subscriptions);

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_options.SubscriptionTtlSeconds),
                SlidingExpiration = TimeSpan.FromSeconds(_options.SubscriptionSlidingExpirationSeconds)
            };

            await _cache.SetAsync(cacheKey, json, cacheOptions, cancellationToken);

            _logger.LogDebug("Cached {Count} subscriptions for key {CacheKey} with TTL {TtlSeconds}s",
                subscriptions.Count, cacheKey, _options.SubscriptionTtlSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache for key {CacheKey}", cacheKey);
            // Don't throw - caching is optional
        }
    }

    /// <summary>
    /// Get cached event type by name.
    /// </summary>
    public async Task<EventType?> GetEventTypeAsync(
        string eventTypeName,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return null;
        }

        var cacheKey = GetEventTypeCacheKey(eventTypeName);

        try
        {
            var cachedBytes = await _cache.GetAsync(cacheKey, cancellationToken);
            if (cachedBytes == null)
            {
                _logger.LogDebug("Cache MISS for key {CacheKey}", cacheKey);
                return null;
            }

            var eventType = JsonSerializer.Deserialize<EventType>(cachedBytes);
            _logger.LogDebug("Cache HIT for key {CacheKey}", cacheKey);

            return eventType;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving from cache for key {CacheKey}", cacheKey);
            return null;
        }
    }

    /// <summary>
    /// Cache event type.
    /// </summary>
    public async Task SetEventTypeAsync(
        EventType eventType,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return;
        }

        var cacheKey = GetEventTypeCacheKey(eventType.Name);

        try
        {
            var json = JsonSerializer.SerializeToUtf8Bytes(eventType);

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_options.EventTypeTtlSeconds)
            };

            await _cache.SetAsync(cacheKey, json, cacheOptions, cancellationToken);

            _logger.LogDebug("Cached event type for key {CacheKey} with TTL {TtlSeconds}s",
                cacheKey, _options.EventTypeTtlSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache for key {CacheKey}", cacheKey);
        }
    }

    /// <summary>
    /// Get cached API key by key hash.
    /// </summary>
    public async Task<ApiKey?> GetApiKeyAsync(
        string keyHash,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return null;
        }

        var cacheKey = GetApiKeyCacheKey(keyHash);

        try
        {
            var cachedBytes = await _cache.GetAsync(cacheKey, cancellationToken);
            if (cachedBytes == null)
            {
                _logger.LogDebug("Cache MISS for key {CacheKey}", cacheKey);
                return null;
            }

            var apiKey = JsonSerializer.Deserialize<ApiKey>(cachedBytes);
            _logger.LogDebug("Cache HIT for key {CacheKey}", cacheKey);

            return apiKey;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving from cache for key {CacheKey}", cacheKey);
            return null;
        }
    }

    /// <summary>
    /// Cache API key.
    /// </summary>
    public async Task SetApiKeyAsync(
        ApiKey apiKey,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return;
        }

        var cacheKey = GetApiKeyCacheKey(apiKey.KeyHash);

        try
        {
            var json = JsonSerializer.SerializeToUtf8Bytes(apiKey);

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_options.ApiKeyTtlSeconds)
            };

            await _cache.SetAsync(cacheKey, json, cacheOptions, cancellationToken);

            _logger.LogDebug("Cached API key for key {CacheKey} with TTL {TtlSeconds}s",
                cacheKey, _options.ApiKeyTtlSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache for key {CacheKey}", cacheKey);
        }
    }

    /// <summary>
    /// Invalidate subscription cache for a tenant and event type.
    /// </summary>
    public async Task InvalidateSubscriptionsAsync(
        Guid tenantId,
        string eventType,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return;
        }

        var cacheKey = GetSubscriptionCacheKey(tenantId, eventType);

        try
        {
            await _cache.RemoveAsync(cacheKey, cancellationToken);
            _logger.LogInformation("Invalidated cache for key {CacheKey}", cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache for key {CacheKey}", cacheKey);
        }
    }

    /// <summary>
    /// Invalidate all subscription caches for a tenant.
    /// </summary>
    public async Task InvalidateAllSubscriptionsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return;
        }

        // In Redis, we can use pattern matching to delete multiple keys
        // Note: This requires StackExchange.Redis and a direct connection
        // For IDistributedCache abstraction, we track keys separately
        _logger.LogInformation("Invalidating all subscriptions for tenant {TenantId}", tenantId);

        // This is a simplified implementation
        // In production, you might want to use Redis key scanning or maintain a key registry
        _logger.LogWarning("Bulk invalidation not fully implemented for IDistributedCache. " +
                          "Consider using direct Redis connection for pattern-based deletion.");
    }

    /// <summary>
    /// Invalidate event type cache.
    /// </summary>
    public async Task InvalidateEventTypeAsync(
        string eventTypeName,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return;
        }

        var cacheKey = GetEventTypeCacheKey(eventTypeName);

        try
        {
            await _cache.RemoveAsync(cacheKey, cancellationToken);
            _logger.LogInformation("Invalidated cache for key {CacheKey}", cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache for key {CacheKey}", cacheKey);
        }
    }

    /// <summary>
    /// Invalidate API key cache.
    /// </summary>
    public async Task InvalidateApiKeyAsync(
        string keyHash,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return;
        }

        var cacheKey = GetApiKeyCacheKey(keyHash);

        try
        {
            await _cache.RemoveAsync(cacheKey, cancellationToken);
            _logger.LogInformation("Invalidated cache for key {CacheKey}", cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache for key {CacheKey}", cacheKey);
        }
    }

    /// <summary>
    /// Get or create cached value using cache-aside pattern.
    /// </summary>
    public async Task<T> GetOrCreateAsync<T>(
        string cacheKey,
        Func<Task<T>> factory,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return await factory();
        }

        try
        {
            // Try to get from cache
            var cachedBytes = await _cache.GetAsync(cacheKey, cancellationToken);
            if (cachedBytes != null)
            {
                var cachedValue = JsonSerializer.Deserialize<T>(cachedBytes);
                if (cachedValue != null)
                {
                    _logger.LogDebug("Cache HIT for key {CacheKey}", cacheKey);
                    return cachedValue;
                }
            }

            // Cache miss - get from factory
            _logger.LogDebug("Cache MISS for key {CacheKey}", cacheKey);
            var value = await factory();

            // Store in cache
            var json = JsonSerializer.SerializeToUtf8Bytes(value);
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl ?? TimeSpan.FromSeconds(_options.DefaultTtlSeconds)
            };

            await _cache.SetAsync(cacheKey, json, cacheOptions, cancellationToken);

            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetOrCreateAsync for key {CacheKey}. Falling back to factory.", cacheKey);
            return await factory();
        }
    }

    // Private helper methods for cache key generation

    private static string GetSubscriptionCacheKey(Guid tenantId, string eventType)
        => $"subscriptions:{tenantId}:{eventType}";

    private static string GetEventTypeCacheKey(string eventTypeName)
        => $"eventtype:{eventTypeName}";

    private static string GetApiKeyCacheKey(string keyHash)
        => $"apikey:{keyHash}";
}

/// <summary>
/// Redis cache configuration options.
/// </summary>
public class RedisCacheOptions
{
    /// <summary>
    /// Enable or disable Redis caching.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Redis connection string.
    /// </summary>
    public string ConnectionString { get; set; } = "localhost:6379";

    /// <summary>
    /// Instance name prefix for cache keys.
    /// </summary>
    public string InstanceName { get; set; } = "HookVerse:";

    /// <summary>
    /// Default TTL for cached items in seconds.
    /// </summary>
    public int DefaultTtlSeconds { get; set; } = 300; // 5 minutes

    /// <summary>
    /// TTL for subscription cache in seconds.
    /// </summary>
    public int SubscriptionTtlSeconds { get; set; } = 300; // 5 minutes

    /// <summary>
    /// Sliding expiration for subscription cache in seconds.
    /// Extends cache lifetime on access.
    /// </summary>
    public int SubscriptionSlidingExpirationSeconds { get; set; } = 60; // 1 minute

    /// <summary>
    /// TTL for event type cache in seconds.
    /// </summary>
    public int EventTypeTtlSeconds { get; set; } = 600; // 10 minutes

    /// <summary>
    /// TTL for API key cache in seconds.
    /// </summary>
    public int ApiKeyTtlSeconds { get; set; } = 300; // 5 minutes

    /// <summary>
    /// Connect timeout in milliseconds.
    /// </summary>
    public int ConnectTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Sync timeout in milliseconds.
    /// </summary>
    public int SyncTimeoutMs { get; set; } = 1000;

    /// <summary>
    /// Enable retry on timeout.
    /// </summary>
    public bool RetryOnTimeout { get; set; } = true;

    /// <summary>
    /// Abort connection on connect failure.
    /// </summary>
    public bool AbortOnConnectFail { get; set; } = false;
}

/// <summary>
/// Interface for Redis cache service.
/// </summary>
public interface IRedisCacheService
{
    Task<List<Subscription>?> GetSubscriptionsAsync(Guid tenantId, string eventType, CancellationToken cancellationToken = default);
    Task SetSubscriptionsAsync(Guid tenantId, string eventType, List<Subscription> subscriptions, CancellationToken cancellationToken = default);
    Task<EventType?> GetEventTypeAsync(string eventTypeName, CancellationToken cancellationToken = default);
    Task SetEventTypeAsync(EventType eventType, CancellationToken cancellationToken = default);
    Task<ApiKey?> GetApiKeyAsync(string keyHash, CancellationToken cancellationToken = default);
    Task SetApiKeyAsync(ApiKey apiKey, CancellationToken cancellationToken = default);
    Task InvalidateSubscriptionsAsync(Guid tenantId, string eventType, CancellationToken cancellationToken = default);
    Task InvalidateAllSubscriptionsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task InvalidateEventTypeAsync(string eventTypeName, CancellationToken cancellationToken = default);
    Task InvalidateApiKeyAsync(string keyHash, CancellationToken cancellationToken = default);
    Task<T> GetOrCreateAsync<T>(string cacheKey, Func<Task<T>> factory, TimeSpan? ttl = null, CancellationToken cancellationToken = default);
}
