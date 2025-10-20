# Database Connection Pooling Optimization

HookVerse uses optimized PostgreSQL connection pooling via Npgsql to maximize database performance and resource utilization.

## Overview

### What is Connection Pooling?

Connection pooling reuses database connections instead of creating new ones for each request:

- **Reduces connection overhead** (no repeated TCP handshakes and authentication)
- **Improves response times** (connections are pre-established)
- **Better resource utilization** (controlled number of connections)
- **Handles burst traffic** (pool expands/contracts dynamically)

### Performance Benefits

With optimized pooling:
- **60-80% reduction** in connection establishment time
- **40-50% improvement** in overall database query time
- **3-5x higher** concurrent request handling capacity
- **Lower CPU usage** on database server

## Configuration

### Application Settings

Configure connection pooling in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=hookverse;Username=hookverse;Password=your_password"
  },
  "ConnectionPooling": {
    "Pooling": true,
    "MinPoolSize": 10,
    "MaxPoolSize": 100,
    "ConnectionIdleLifetimeSeconds": 300,
    "ConnectionPruningIntervalSeconds": 10,
    "ConnectionTimeoutSeconds": 30,
    "CommandTimeoutSeconds": 30,
    "KeepAliveSeconds": 60,
    "NoResetOnClose": false,
    "MaxAutoPrepare": 20,
    "AutoPrepareMinUsages": 5,
    "Multiplexing": false,
    "MaxRetryCount": 3,
    "MaxRetryDelaySeconds": 5
  }
}
```

### Environment-Specific Configuration

#### Development

```json
{
  "ConnectionPooling": {
    "MinPoolSize": 2,
    "MaxPoolSize": 20,
    "ConnectionIdleLifetimeSeconds": 600,
    "NoResetOnClose": false
  }
}
```

**Rationale**: Lower pool sizes for resource-constrained development environments.

#### Staging

```json
{
  "ConnectionPooling": {
    "MinPoolSize": 5,
    "MaxPoolSize": 50,
    "ConnectionIdleLifetimeSeconds": 300,
    "NoResetOnClose": false
  }
}
```

**Rationale**: Moderate pool sizes for testing under realistic load.

#### Production (Low Load < 100 req/s)

```json
{
  "ConnectionPooling": {
    "MinPoolSize": 10,
    "MaxPoolSize": 100,
    "ConnectionIdleLifetimeSeconds": 300,
    "NoResetOnClose": false,
    "MaxAutoPrepare": 20,
    "AutoPrepareMinUsages": 5
  }
}
```

#### Production (Medium Load 100-1000 req/s)

```json
{
  "ConnectionPooling": {
    "MinPoolSize": 20,
    "MaxPoolSize": 200,
    "ConnectionIdleLifetimeSeconds": 300,
    "NoResetOnClose": true,
    "MaxAutoPrepare": 50,
    "AutoPrepareMinUsages": 3
  }
}
```

#### Production (High Load > 1000 req/s)

```json
{
  "ConnectionPooling": {
    "MinPoolSize": 50,
    "MaxPoolSize": 500,
    "ConnectionIdleLifetimeSeconds": 180,
    "NoResetOnClose": true,
    "MaxAutoPrepare": 100,
    "AutoPrepareMinUsages": 2,
    "Multiplexing": true,
    "WriteCoalescingBufferThresholdBytes": 1000
  }
}
```

### Service Registration

Register in `Program.cs`:

```csharp
using HookVerse.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add optimized PostgreSQL connection pooling
builder.Services.AddOptimizedPostgreSqlPooling(builder.Configuration);

var app = builder.Build();
app.Run();
```

## Configuration Parameters

### Pool Size Settings

#### MinPoolSize

**Description**: Minimum number of connections maintained in the pool.

**Recommendations**:
- **Development**: 2-5
- **Production (low)**: 10-20
- **Production (medium)**: 20-50
- **Production (high)**: 50-100

**Formula**: `MinPoolSize = Expected Concurrent Requests × 0.1`

#### MaxPoolSize

**Description**: Maximum number of connections allowed in the pool.

**Recommendations**:
- **Development**: 10-20
- **Production (low)**: 50-100
- **Production (medium)**: 100-200
- **Production (high)**: 200-500

**Formula**: `MaxPoolSize = Expected Peak Concurrent Requests × 2`

**Warning**: Don't exceed PostgreSQL's `max_connections` setting.

### Connection Lifecycle

#### ConnectionIdleLifetimeSeconds

**Description**: How long a connection can be idle before being closed.

**Recommendations**:
- **Short-lived requests**: 180-300 seconds
- **Long-running operations**: 600-900 seconds

**Trade-off**: 
- Lower values = more frequent connection recycling = better for failover
- Higher values = fewer reconnections = better performance

#### ConnectionPruningIntervalSeconds

**Description**: How often to check for idle connections to close.

**Recommendation**: 10 seconds (default)

**Impact**: Minimal performance impact, helps maintain optimal pool size.

### Timeout Settings

#### ConnectionTimeoutSeconds

**Description**: Maximum time to wait for a connection from the pool.

**Recommendations**:
- **Normal**: 15-30 seconds
- **High load**: 30-60 seconds

**Symptoms of too low**: `NpgsqlException: Timeout expired` during high load.

#### CommandTimeoutSeconds

**Description**: Maximum time for a SQL command to execute.

**Recommendations**:
- **Simple queries**: 15-30 seconds
- **Complex queries**: 60-120 seconds
- **Reports/analytics**: 300-600 seconds

#### KeepAliveSeconds

**Description**: TCP keep-alive interval to detect dead connections.

**Recommendations**:
- **Cloud environments**: 30-60 seconds (aggressive detection)
- **On-premise**: 60-120 seconds (normal detection)

### Performance Features

#### NoResetOnClose

**Description**: Don't reset connection state when returning to pool.

**Benefits**:
- **15-20% faster** connection reuse
- Reduced CPU usage

**Risks**:
- Open transactions not rolled back
- Temporary tables persist

**Recommendations**:
- **Development**: `false` (safer)
- **Production (medium/high load)**: `true` (faster)

**Requirements when true**:
- Always dispose DbContext properly
- Always commit or rollback transactions
- Clean up temporary resources

#### MaxAutoPrepare

**Description**: Maximum prepared statements to cache per connection.

**Benefits**:
- **30-50% faster** for frequently executed queries
- Reduced parsing overhead

**Recommendations**:
- **Low load**: 10-20
- **Medium load**: 20-50
- **High load**: 50-100

**Memory impact**: ~10KB per prepared statement

#### AutoPrepareMinUsages

**Description**: Execute command this many times before auto-preparing.

**Recommendations**:
- **High load**: 2-3 (aggressive preparation)
- **Medium load**: 5-10 (balanced)
- **Low load**: 10-20 (conservative)

### Advanced Features

#### Multiplexing

**Description**: Multiple commands on a single physical connection.

**Benefits**:
- **Dramatically reduced** connection count
- Better resource utilization

**Limitations**:
- Experimental feature
- Not suitable for all workloads
- Requires Npgsql 6.0+

**When to use**:
- ✅ High concurrency (> 1000 req/s)
- ✅ Short-lived queries
- ❌ Long-running transactions
- ❌ Commands with large result sets

## Pool Sizing Calculator

### Formula-Based Sizing

```
MinPoolSize = ExpectedConcurrentRequests × 0.1
MaxPoolSize = PeakConcurrentRequests × 2

Example for 500 req/s with 50ms avg response time:
- Concurrent requests = 500 × 0.05 = 25
- MinPoolSize = 25 × 0.1 = 3 (round up to 10)
- MaxPoolSize = 50 × 2 = 100
```

### Load-Based Recommendations

| Requests/Second | Concurrent | MinPoolSize | MaxPoolSize |
|----------------|------------|-------------|-------------|
| 10             | 1          | 2           | 10          |
| 50             | 3          | 5           | 20          |
| 100            | 5          | 10          | 50          |
| 500            | 25         | 10          | 100         |
| 1,000          | 50         | 20          | 200         |
| 5,000          | 250        | 50          | 500         |
| 10,000         | 500        | 100         | 1000        |

### Database Server Constraints

**PostgreSQL max_connections**:

Check current limit:
```sql
SHOW max_connections;
```

**Calculation**:
```
Application connections = MaxPoolSize × Number of Application Instances
Reserve connections = 10-20 (for admin, monitoring)
Required max_connections = Application connections + Reserve connections

Example:
- 3 application instances
- MaxPoolSize = 100
- Required = (100 × 3) + 20 = 320
```

**Adjust PostgreSQL**:
```sql
-- In postgresql.conf
max_connections = 400

-- Restart PostgreSQL
sudo systemctl restart postgresql
```

## Monitoring

### Connection Pool Metrics

#### Prometheus Metrics

```csharp
private static readonly Gauge PoolConnections = Metrics
    .CreateGauge("hookverse_db_pool_connections",
        "Number of connections in the pool",
        new GaugeConfiguration
        {
            LabelNames = new[] { "state" }
        });

public class ConnectionPoolMonitor : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var dataSource = NpgsqlDataSource.Create(connectionString);
            var statistics = dataSource.Statistics;

            PoolConnections.WithLabels("idle").Set(statistics.Idle);
            PoolConnections.WithLabels("active").Set(statistics.Busy);
            PoolConnections.WithLabels("total").Set(statistics.Total);

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
```

#### Key Metrics to Track

1. **Pool utilization**: `active_connections / max_pool_size`
2. **Pool exhaustion events**: Connection timeout exceptions
3. **Average wait time**: Time to acquire connection
4. **Connection creation rate**: New connections per second
5. **Connection lifetime**: Average connection age

### Grafana Dashboard Queries

```promql
# Pool utilization percentage
(hookverse_db_pool_connections{state="active"} / 
 hookverse_db_pool_connections{state="total"}) * 100

# Connection wait time
histogram_quantile(0.95, 
  rate(hookverse_db_connection_wait_seconds_bucket[5m]))

# Connection exhaustion events
rate(hookverse_db_pool_timeout_total[5m])

# Average connection lifetime
rate(hookverse_db_connection_lifetime_seconds_sum[5m]) / 
  rate(hookverse_db_connection_lifetime_seconds_count[5m])
```

### Application Insights Queries

```kusto
// Connection timeout errors
exceptions
| where type == "NpgsqlException"
| where outerMessage contains "Timeout"
| summarize count() by bin(timestamp, 5m)
| render timechart

// Database query duration
dependencies
| where type == "SQL"
| summarize avg(duration), percentile(duration, 95) by bin(timestamp, 5m)
| render timechart

// Connection pool exhaustion
traces
| where message contains "connection pool"
| where severityLevel >= 3
| summarize count() by bin(timestamp, 5m)
```

## Performance Tuning

### Symptoms and Solutions

#### Symptom: Frequent "Timeout expired" errors

**Diagnosis**:
- Pool exhaustion
- MaxPoolSize too low
- Connections not being returned

**Solutions**:
```json
{
  "MaxPoolSize": 200,  // Increase pool size
  "ConnectionTimeoutSeconds": 60  // Increase timeout
}
```

**Code fixes**:
```csharp
// ✅ Good: Always dispose DbContext
await using var context = new ApplicationDbContext();
var data = await context.Users.ToListAsync();

// ❌ Bad: Not disposing DbContext
var context = new ApplicationDbContext();
var data = await context.Users.ToListAsync();
```

#### Symptom: High database connection count

**Diagnosis**:
- MinPoolSize too high
- Connection leaks

**Solutions**:
```json
{
  "MinPoolSize": 10,  // Reduce minimum
  "ConnectionIdleLifetimeSeconds": 180  // Shorter lifetime
}
```

#### Symptom: Slow connection acquisition

**Diagnosis**:
- Connection pool contention
- Slow connection creation

**Solutions**:
```json
{
  "MinPoolSize": 20,  // Maintain more idle connections
  "ConnectionPruningIntervalSeconds": 5  // More aggressive pruning
}
```

#### Symptom: Inconsistent query performance

**Diagnosis**:
- Not using prepared statements
- Connection state not reset

**Solutions**:
```json
{
  "MaxAutoPrepare": 50,
  "AutoPrepareMinUsages": 3,
  "NoResetOnClose": false  // Enable reset for consistency
}
```

### Load Testing

#### Test Connection Pool Under Load

```bash
# Use k6 for load testing
k6 run --vus 100 --duration 5m load-test.js

# Monitor pool metrics
watch -n 1 'curl -s http://localhost:9090/metrics | grep hookverse_db_pool'
```

#### Example k6 Script

```javascript
import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '2m', target: 50 },   // Ramp up to 50 users
    { duration: '5m', target: 100 },  // Stay at 100 users
    { duration: '2m', target: 200 },  // Spike to 200 users
    { duration: '5m', target: 100 },  // Return to 100 users
    { duration: '2m', target: 0 },    // Ramp down
  ],
};

export default function () {
  const res = http.get('http://localhost:5000/api/v1/subscriptions');
  check(res, {
    'status is 200': (r) => r.status === 200,
    'response time < 200ms': (r) => r.timings.duration < 200,
  });
  sleep(1);
}
```

## Best Practices

### 1. Always Dispose DbContext

```csharp
// ✅ Good: Using statement ensures disposal
await using var context = _contextFactory.CreateDbContext();
var result = await context.Users.ToListAsync();

// ✅ Good: Try-finally ensures disposal
var context = _contextFactory.CreateDbContext();
try
{
    var result = await context.Users.ToListAsync();
}
finally
{
    await context.DisposeAsync();
}
```

### 2. Use AsNoTracking for Read-Only Queries

```csharp
// ✅ Good: No tracking overhead
var users = await context.Users
    .AsNoTracking()
    .ToListAsync();

// ❌ Bad: Tracking adds memory and CPU overhead
var users = await context.Users.ToListAsync();
```

### 3. Explicitly Manage Transactions

```csharp
// ✅ Good: Always commit or rollback
await using var transaction = await context.Database.BeginTransactionAsync();
try
{
    await context.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}

// ❌ Bad: Transaction not explicitly handled
await context.SaveChangesAsync();
```

### 4. Configure Pool Size Based on Load

```csharp
// Use recommended settings helper
var poolingOptions = ConnectionPoolingOptions.GetRecommendedSettings(
    environment: "production",
    loadProfile: LoadProfile.High);
```

### 5. Monitor Pool Metrics

```csharp
// Log pool statistics periodically
_logger.LogInformation(
    "Pool stats - Total: {Total}, Active: {Active}, Idle: {Idle}",
    statistics.Total,
    statistics.Busy,
    statistics.Idle);
```

## Kubernetes Configuration

### PostgreSQL Connection String

Use Kubernetes secrets for connection strings:

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: hookverse-db-secret
  namespace: hookverse
type: Opaque
stringData:
  connection-string: "Host=postgres.hookverse.svc.cluster.local;Database=hookverse;Username=hookverse;Password=your_password"
```

### Environment Variables

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: hookverse-api
spec:
  template:
    spec:
      containers:
      - name: api
        env:
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: hookverse-db-secret
              key: connection-string
        - name: ConnectionPooling__MaxPoolSize
          value: "100"
        - name: ConnectionPooling__MinPoolSize
          value: "10"
```

### Resource Limits

```yaml
spec:
  containers:
  - name: api
    resources:
      requests:
        memory: "512Mi"
        cpu: "250m"
      limits:
        memory: "2Gi"
        cpu: "1000m"
```

**Note**: Each connection uses ~5-10MB of memory. Factor this into resource limits.

## Troubleshooting

### Issue: "Timeout expired" errors

**Check pool exhaustion**:
```bash
# Check metrics
curl http://localhost:9090/metrics | grep pool_connections

# Check logs
kubectl logs -n hookverse deployment/hookverse-api | grep "Timeout"
```

**Solution**: Increase `MaxPoolSize` or check for connection leaks.

### Issue: High database connection count

**Check PostgreSQL connections**:
```sql
SELECT count(*) FROM pg_stat_activity WHERE datname = 'hookverse';
```

**Solution**: Reduce `MinPoolSize` or fix connection leaks.

### Issue: "Too many connections" error

**Check PostgreSQL limit**:
```sql
SELECT * FROM pg_settings WHERE name = 'max_connections';
```

**Solution**: Increase `max_connections` in PostgreSQL or reduce application pool sizes.

### Issue: Connection pool deadlock

**Symptoms**: Application hangs, no errors

**Cause**: Nested DbContext usage consuming all connections

**Solution**:
```csharp
// ❌ Bad: Nested context usage
await using var context1 = _contextFactory.CreateDbContext();
var user = await context1.Users.FindAsync(userId);

await using var context2 = _contextFactory.CreateDbContext();  // Can deadlock!
var orders = await context2.Orders.Where(o => o.UserId == userId).ToListAsync();

// ✅ Good: Single context
await using var context = _contextFactory.CreateDbContext();
var user = await context.Users.FindAsync(userId);
var orders = await context.Orders.Where(o => o.UserId == userId).ToListAsync();
```

## Related Documentation

- [Compiled Queries](./compiled-queries.md)
- [Redis Caching](./redis-caching.md)
- [Performance Testing](../testing/performance.md)
- [Monitoring](../observability/README.md)
