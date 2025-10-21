# Redis Caching Layer

HookVerse uses Redis as a distributed cache to improve performance for frequently accessed data, particularly subscription lookups during webhook delivery.

## Overview

### Benefits

- **70-90% reduction** in database queries for subscription lookups
- **5-10x faster** response times for cached data
- **Horizontal scalability** across multiple application instances
- **Shared cache** between all pods in Kubernetes cluster
- **Automatic expiration** with configurable TTL values

### Caching Strategy

HookVerse implements the **cache-aside** pattern:

1. Check Redis cache first
2. On cache miss, query database
3. Store result in cache
4. Return result

## Configuration

### Application Settings

Configure Redis in `appsettings.json`:

```json
{
  "RedisCache": {
    "Enabled": true,
    "ConnectionString": "localhost:6379",
    "InstanceName": "HookVerse:",
    "DefaultTtlSeconds": 300,
    "SubscriptionTtlSeconds": 300,
    "SubscriptionSlidingExpirationSeconds": 60,
    "EventTypeTtlSeconds": 600,
    "ApiKeyTtlSeconds": 300,
    "ConnectTimeoutMs": 5000,
    "SyncTimeoutMs": 1000,
    "RetryOnTimeout": true,
    "AbortOnConnectFail": false
  }
}
```

### Environment-Specific Configuration

#### Development

```json
{
  "RedisCache": {
    "Enabled": false
  }
}
```

Uses in-memory cache as fallback when Redis is disabled.

#### Production

```json
{
  "RedisCache": {
    "Enabled": true,
    "ConnectionString": "redis-master.hookverse.svc.cluster.local:6379,password=${REDIS_PASSWORD}",
    "InstanceName": "HookVerse:prod:",
    "SubscriptionTtlSeconds": 300,
    "ConnectTimeoutMs": 5000,
    "RetryOnTimeout": true
  }
}
```

### Service Registration

Register Redis caching in `Program.cs`:

```csharp
using HookVerse.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Redis distributed caching
builder.Services.AddRedisDistributedCaching(builder.Configuration);

var app = builder.Build();
app.Run();
```

## Usage Examples

### Subscription Caching

#### Repository with Cache-Aside Pattern

```csharp
public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly ApplicationDbContext _context;
    private readonly IRedisCacheService _cache;
    private readonly ILogger<SubscriptionRepository> _logger;

    public SubscriptionRepository(
        ApplicationDbContext context,
        IRedisCacheService cache,
        ILogger<SubscriptionRepository> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<Subscription>> GetActiveSubscriptionsByEventTypeAsync(
        Guid tenantId,
        string eventType,
        CancellationToken cancellationToken = default)
    {
        // Try to get from cache
        var cached = await _cache.GetSubscriptionsAsync(
            tenantId, eventType, cancellationToken);

        if (cached != null)
        {
            _logger.LogDebug("Subscriptions retrieved from cache");
            return cached;
        }

        // Cache miss - query database
        var subscriptions = await CompiledQueries
            .GetActiveSubscriptionsByEventType(_context, tenantId, eventType)
            .ToListAsync(cancellationToken);

        // Store in cache
        await _cache.SetSubscriptionsAsync(
            tenantId, eventType, subscriptions, cancellationToken);

        _logger.LogDebug("Subscriptions retrieved from database and cached");
        return subscriptions;
    }

    public async Task<Subscription> UpdateSubscriptionAsync(
        Subscription subscription,
        CancellationToken cancellationToken = default)
    {
        _context.Subscriptions.Update(subscription);
        await _context.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        await _cache.InvalidateSubscriptionsAsync(
            subscription.TenantId,
            subscription.EventType.Name,
            cancellationToken);

        _logger.LogInformation(
            "Updated subscription {SubscriptionId} and invalidated cache",
            subscription.Id);

        return subscription;
    }
}
```

### Event Type Caching

```csharp
public class EventTypeRepository : IEventTypeRepository
{
    private readonly ApplicationDbContext _context;
    private readonly IRedisCacheService _cache;

    public async Task<EventType?> GetByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        // Try cache first
        var cached = await _cache.GetEventTypeAsync(name, cancellationToken);
        if (cached != null)
        {
            return cached;
        }

        // Query database
        var eventType = await CompiledQueries
            .GetEventTypeByName(_context, name);

        if (eventType != null)
        {
            // Cache the result
            await _cache.SetEventTypeAsync(eventType, cancellationToken);
        }

        return eventType;
    }

    public async Task<EventType> UpdateAsync(
        EventType eventType,
        CancellationToken cancellationToken = default)
    {
        _context.EventTypes.Update(eventType);
        await _context.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        await _cache.InvalidateEventTypeAsync(eventType.Name, cancellationToken);

        return eventType;
    }
}
```

### API Key Caching

```csharp
public class ApiKeyAuthenticationHandler
{
    private readonly ApplicationDbContext _context;
    private readonly IRedisCacheService _cache;

    public async Task<ApiKey?> ValidateApiKeyAsync(
        string keyHash,
        CancellationToken cancellationToken = default)
    {
        // Try cache first (very hot path)
        var cached = await _cache.GetApiKeyAsync(keyHash, cancellationToken);
        if (cached != null)
        {
            return cached;
        }

        // Query database
        var apiKey = await CompiledQueries
            .GetApiKeyByKeyHash(_context, keyHash);

        if (apiKey != null && apiKey.IsActive)
        {
            // Cache valid API keys
            await _cache.SetApiKeyAsync(apiKey, cancellationToken);
        }

        return apiKey;
    }

    public async Task RevokeApiKeyAsync(
        Guid apiKeyId,
        string keyHash,
        CancellationToken cancellationToken = default)
    {
        // Revoke in database
        var apiKey = await _context.ApiKeys.FindAsync(apiKeyId);
        if (apiKey != null)
        {
            apiKey.IsActive = false;
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Immediately invalidate cache
        await _cache.InvalidateApiKeyAsync(keyHash, cancellationToken);
    }
}
```

### Generic Cache-Aside Pattern

```csharp
public class TenantService
{
    private readonly IRedisCacheService _cache;
    private readonly ITenantRepository _repository;

    public async Task<Tenant?> GetTenantAsync(Guid id)
    {
        return await _cache.GetOrCreateAsync(
            cacheKey: $"tenant:{id}",
            factory: () => _repository.GetByIdAsync(id),
            ttl: TimeSpan.FromMinutes(10));
    }
}
```

## Cache Invalidation

### When to Invalidate

Invalidate cache immediately after:

1. **Create**: New subscription, event type, or API key
2. **Update**: Modified subscription, event type, or API key
3. **Delete**: Deleted subscription, event type, or revoked API key
4. **State Changes**: Subscription active/inactive, API key revoked

### Invalidation Patterns

#### Single Item Invalidation

```csharp
public async Task UpdateSubscriptionAsync(Subscription subscription)
{
    // Update database
    await _repository.UpdateAsync(subscription);

    // Invalidate specific cache entry
    await _cache.InvalidateSubscriptionsAsync(
        subscription.TenantId,
        subscription.EventType.Name);
}
```

#### Related Items Invalidation

```csharp
public async Task UpdateEventTypeAsync(EventType eventType)
{
    // Update database
    await _repository.UpdateAsync(eventType);

    // Invalidate event type cache
    await _cache.InvalidateEventTypeAsync(eventType.Name);

    // Also invalidate all subscriptions using this event type
    // This requires keeping track of affected tenants
    foreach (var tenantId in affectedTenants)
    {
        await _cache.InvalidateSubscriptionsAsync(tenantId, eventType.Name);
    }
}
```

#### Bulk Invalidation

```csharp
public async Task DeleteTenantAsync(Guid tenantId)
{
    // Delete from database
    await _repository.DeleteAsync(tenantId);

    // Invalidate all tenant-related caches
    await _cache.InvalidateAllSubscriptionsAsync(tenantId);
}
```

## Performance Optimization

### Cache Hit Rates

Monitor cache effectiveness:

```csharp
public class CacheMetricsService
{
    private readonly Counter<long> _cacheHits;
    private readonly Counter<long> _cacheMisses;

    public void RecordCacheHit(string cacheType)
    {
        _cacheHits.Add(1, new KeyValuePair<string, object?>("cache.type", cacheType));
    }

    public void RecordCacheMiss(string cacheType)
    {
        _cacheMisses.Add(1, new KeyValuePair<string, object?>("cache.type", cacheType));
    }
}
```

Target cache hit rates:
- **Subscriptions**: 80-90% (hot path)
- **Event Types**: 90-95% (rarely change)
- **API Keys**: 85-95% (authentication)

### TTL Optimization

#### Short TTL (60-300 seconds)

Use for frequently changing data:
- Active subscriptions (5 minutes)
- API keys (5 minutes)

#### Medium TTL (300-600 seconds)

Use for moderately stable data:
- Event types (10 minutes)
- Tenant configurations (5 minutes)

#### Long TTL (600-3600 seconds)

Use for static data:
- System configurations (30 minutes)
- Schema definitions (1 hour)

### Sliding Expiration

Extend cache lifetime for frequently accessed items:

```csharp
var cacheOptions = new DistributedCacheEntryOptions
{
    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
    SlidingExpiration = TimeSpan.FromMinutes(1)
};
```

## Kubernetes Deployment

### Redis Deployment

Create `redis-deployment.yaml`:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: redis
  namespace: hookverse
spec:
  replicas: 1
  selector:
    matchLabels:
      app: redis
  template:
    metadata:
      labels:
        app: redis
    spec:
      containers:
      - name: redis
        image: redis:7-alpine
        ports:
        - containerPort: 6379
        resources:
          requests:
            memory: "256Mi"
            cpu: "100m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        volumeMounts:
        - name: redis-data
          mountPath: /data
        command:
        - redis-server
        - --appendonly yes
        - --maxmemory 256mb
        - --maxmemory-policy allkeys-lru
      volumes:
      - name: redis-data
        persistentVolumeClaim:
          claimName: redis-pvc
---
apiVersion: v1
kind: Service
metadata:
  name: redis
  namespace: hookverse
spec:
  selector:
    app: redis
  ports:
  - port: 6379
    targetPort: 6379
  type: ClusterIP
---
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: redis-pvc
  namespace: hookverse
spec:
  accessModes:
  - ReadWriteOnce
  resources:
    requests:
      storage: 10Gi
```

### Redis Configuration Secret

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: redis-secret
  namespace: hookverse
type: Opaque
stringData:
  password: "your-redis-password-here"
```

### Application Configuration

Update ConfigMap:

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: hookverse-config
  namespace: hookverse
data:
  appsettings.Production.json: |
    {
      "RedisCache": {
        "Enabled": true,
        "ConnectionString": "redis.hookverse.svc.cluster.local:6379,password=${REDIS_PASSWORD}",
        "InstanceName": "HookVerse:prod:",
        "SubscriptionTtlSeconds": 300
      }
    }
```

### High Availability Setup

For production, use Redis Sentinel or Redis Cluster:

```yaml
apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: redis-sentinel
  namespace: hookverse
spec:
  serviceName: redis-sentinel
  replicas: 3
  selector:
    matchLabels:
      app: redis-sentinel
  template:
    metadata:
      labels:
        app: redis-sentinel
    spec:
      containers:
      - name: redis
        image: redis:7-alpine
        ports:
        - containerPort: 6379
        - containerPort: 26379
        env:
        - name: REDIS_REPLICATION_MODE
          value: "sentinel"
```

## Monitoring

### Prometheus Metrics

```promql
# Cache hit rate
rate(hookverse_cache_hits_total[5m]) / 
  (rate(hookverse_cache_hits_total[5m]) + rate(hookverse_cache_misses_total[5m]))

# Cache operations
rate(hookverse_cache_operations_total[5m])

# Redis connection status
up{job="redis"}

# Redis memory usage
redis_memory_used_bytes / redis_memory_max_bytes
```

### Grafana Dashboard

Key metrics to monitor:

- Cache hit/miss rate
- Average response time (cached vs uncached)
- Redis memory usage
- Redis connection count
- Cache eviction rate
- TTL distribution

### Health Checks

```csharp
public class RedisCacheHealthCheck : IHealthCheck
{
    private readonly IDistributedCache _cache;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var testKey = "health:check";
            var testValue = Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString());

            await _cache.SetAsync(testKey, testValue, cancellationToken);
            var retrieved = await _cache.GetAsync(testKey, cancellationToken);

            if (retrieved != null)
            {
                await _cache.RemoveAsync(testKey, cancellationToken);
                return HealthCheckResult.Healthy("Redis cache is responsive");
            }

            return HealthCheckResult.Degraded("Redis cache write/read failed");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis cache is unavailable", ex);
        }
    }
}
```

## Best Practices

### 1. Always Handle Cache Failures Gracefully

```csharp
// ✅ Good: Fallback to database on cache failure
try
{
    cached = await _cache.GetAsync(key);
}
catch (Exception ex)
{
    _logger.LogWarning(ex, "Cache failure, falling back to database");
    cached = null;
}

if (cached == null)
{
    return await _repository.GetAsync(id);
}
```

### 2. Use Appropriate TTL Values

```csharp
// ✅ Good: Short TTL for frequently changing data
SubscriptionTtlSeconds = 300  // 5 minutes

// ✅ Good: Longer TTL for stable data
EventTypeTtlSeconds = 600  // 10 minutes
```

### 3. Invalidate Aggressively

```csharp
// ✅ Good: Invalidate immediately after write
await _repository.UpdateAsync(subscription);
await _cache.InvalidateAsync(subscription.Id);

// ❌ Bad: Relying only on TTL expiration
await _repository.UpdateAsync(subscription);
// Cache will serve stale data until TTL expires
```

### 4. Use Consistent Key Naming

```csharp
// ✅ Good: Hierarchical, descriptive keys
$"subscriptions:{tenantId}:{eventType}"
$"eventtype:{eventTypeName}"
$"apikey:{keyHash}"

// ❌ Bad: Generic, unclear keys
$"sub:{id}"
$"data:{key}"
```

### 5. Monitor Cache Performance

```csharp
// ✅ Good: Track cache operations
_logger.LogDebug("Cache {Status} for key {Key} in {Duration}ms",
    hit ? "HIT" : "MISS", cacheKey, stopwatch.ElapsedMilliseconds);
```

## Troubleshooting

### Issue: Cache misses are high

**Possible causes**:
1. TTL too short
2. Cache being invalidated too frequently
3. Redis memory limit reached (evictions)

**Solution**:
```bash
# Check Redis memory usage
redis-cli INFO memory

# Check eviction policy
redis-cli CONFIG GET maxmemory-policy

# Increase TTL
"SubscriptionTtlSeconds": 600
```

### Issue: Stale data in cache

**Possible causes**:
1. Cache invalidation not working
2. Multiple application instances
3. Manual database updates

**Solution**:
- Ensure all write operations invalidate cache
- Use Redis pub/sub for cross-instance invalidation
- Reduce TTL values

### Issue: Redis connection failures

**Possible causes**:
1. Redis server down
2. Network issues
3. Connection pool exhausted

**Solution**:
```json
{
  "RedisCache": {
    "ConnectTimeoutMs": 5000,
    "SyncTimeoutMs": 1000,
    "RetryOnTimeout": true,
    "AbortOnConnectFail": false
  }
}
```

### Issue: Memory pressure in Redis

**Solution**:
```bash
# Set memory limit
redis-cli CONFIG SET maxmemory 256mb

# Set eviction policy
redis-cli CONFIG SET maxmemory-policy allkeys-lru

# Monitor memory
redis-cli INFO memory | grep used_memory_human
```

## Related Documentation

- [Response Caching](./response-caching.md)
- [Compiled Queries](./compiled-queries.md)
- [Database Optimization](./database-optimization.md)
- [Monitoring](../observability/README.md)
