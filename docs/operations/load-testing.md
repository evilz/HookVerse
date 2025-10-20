# Load Testing Guide

Comprehensive load testing for HookVerse webhook delivery platform targeting 10,000 webhooks/second.

## Overview

Load testing validates that HookVerse can handle high-throughput webhook delivery scenarios. The target performance is **10,000 webhooks per second** with:

- **P95 response time < 500ms** for webhook submission
- **Error rate < 1%** under peak load
- **P99 end-to-end delivery < 5 seconds**

## Prerequisites

### Required Tools

1. **k6**: Modern load testing tool
   ```bash
   # Windows (Chocolatey)
   choco install k6
   
   # macOS (Homebrew)
   brew install k6
   
   # Linux
   sudo gpg -k
   sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D00
   echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
   sudo apt-get update
   sudo apt-get install k6
   ```

2. **HookVerse API**: Running instance with database and message broker

3. **Monitoring**: Grafana + Prometheus for observability

### System Requirements

For 10K req/s load test:

- **API Pods**: 3-5 replicas with 2 CPU, 4GB RAM each
- **Worker Pods**: 5-10 replicas with 1 CPU, 2GB RAM each
- **PostgreSQL**: 4 CPU, 8GB RAM minimum
- **RabbitMQ**: 2 CPU, 4GB RAM minimum
- **Redis**: 2 CPU, 4GB RAM minimum

## Running Load Tests

### Quick Start

Run default 10K req/s load test:

```powershell
.\.specify\scripts\powershell\run-load-test.ps1
```

### Custom Configuration

```powershell
# 5K req/s for 15 minutes
.\.specify\scripts\powershell\run-load-test.ps1 `
    -Target 5000 `
    -Duration 15

# Custom API URL and key
.\.specify\scripts\powershell\run-load-test.ps1 `
    -ApiUrl "https://api.hookverse.example.com" `
    -ApiKey "your-api-key"

# Skip setup checks
.\.specify\scripts\powershell\run-load-test.ps1 -SkipSetup
```

### Direct k6 Execution

For advanced scenarios, run k6 directly:

```bash
# Set environment variables
export API_URL=http://localhost:5000
export API_KEY=test-api-key

# Run load test
k6 run --out json=metrics.json .specify/scripts/k6/load-test.js

# Run with custom stages
k6 run --stage 2m:1000,5m:5000,10m:10000 .specify/scripts/k6/load-test.js
```

## Test Scenarios

### Scenario 1: Baseline Performance (1K req/s)

Validates basic system functionality:

```javascript
export const options = {
  stages: [
    { duration: '2m', target: 1000 },
    { duration: '10m', target: 1000 },
    { duration: '2m', target: 0 },
  ],
};
```

**Expected Results**:
- P95 response time: < 100ms
- Error rate: < 0.1%
- CPU usage: < 30%

### Scenario 2: Standard Load (5K req/s)

Typical production traffic:

```javascript
export const options = {
  stages: [
    { duration: '3m', target: 5000 },
    { duration: '15m', target: 5000 },
    { duration: '2m', target: 0 },
  ],
};
```

**Expected Results**:
- P95 response time: < 200ms
- Error rate: < 0.5%
- CPU usage: < 60%

### Scenario 3: Peak Load (10K req/s)

Maximum sustained throughput:

```javascript
export const options = {
  stages: [
    { duration: '5m', target: 10000 },
    { duration: '20m', target: 10000 },
    { duration: '3m', target: 0 },
  ],
};
```

**Expected Results**:
- P95 response time: < 500ms
- Error rate: < 1%
- CPU usage: < 80%

### Scenario 4: Spike Test (15K req/s)

Sudden traffic spike handling:

```javascript
export const options = {
  stages: [
    { duration: '2m', target: 5000 },
    { duration: '1m', target: 15000 },  // Sudden spike
    { duration: '5m', target: 15000 },
    { duration: '2m', target: 5000 },
    { duration: '2m', target: 0 },
  ],
};
```

**Expected Results**:
- System should handle spike without crashes
- Error rate may increase temporarily (< 2%)
- Recovery within 1 minute

### Scenario 5: Soak Test (24 hours)

Long-running stability test:

```javascript
export const options = {
  stages: [
    { duration: '10m', target: 5000 },
    { duration: '23h', target: 5000 },
    { duration: '10m', target: 0 },
  ],
};
```

**Expected Results**:
- No memory leaks
- No performance degradation
- Consistent response times

## Metrics and Thresholds

### HTTP Metrics

```javascript
thresholds: {
  // Response time
  'http_req_duration': ['p(95)<500', 'p(99)<1000'],
  
  // Request rate
  'http_reqs': ['rate>9000'],  // At least 9K req/s (90% of target)
  
  // Error rate
  'http_req_failed': ['rate<0.01'],  // < 1% errors
  
  // Connection time
  'http_req_connecting': ['p(95)<100'],
}
```

### Custom Metrics

```javascript
// Webhook-specific metrics
const webhookDeliveryTime = new Trend('webhook_delivery_time');
const webhooksSent = new Counter('webhooks_sent');
const webhooksDelivered = new Counter('webhooks_delivered');
const webhooksFailed = new Counter('webhooks_failed');

thresholds: {
  'webhook_delivery_time': ['p(95)<2000', 'p(99)<5000'],
}
```

### System Metrics (from Prometheus)

Monitor these during load test:

```promql
# Request rate
rate(http_requests_total[1m])

# Response time
histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m]))

# Error rate
rate(http_requests_total{status=~"5.."}[1m]) / rate(http_requests_total[1m])

# Database connections
pg_stat_activity_count

# Message queue depth
rabbitmq_queue_messages{queue="webhooks"}

# CPU usage
rate(process_cpu_seconds_total[5m]) * 100

# Memory usage
process_resident_memory_bytes / 1024 / 1024
```

## Interpreting Results

### Success Criteria

Load test **PASSES** if:

✅ Request rate >= 9,000 req/s (90% of 10K target)  
✅ P95 response time < 500ms  
✅ P99 response time < 1000ms  
✅ Error rate < 1%  
✅ No pod crashes or restarts  
✅ Database connections stable  
✅ Message queue processing keeps up

### Warning Signs

Load test **NEEDS INVESTIGATION** if:

⚠️ Request rate 8,000-9,000 req/s (80-90% of target)  
⚠️ P95 response time 500-800ms  
⚠️ Error rate 1-2%  
⚠️ CPU usage > 85%  
⚠️ Memory usage growing over time

### Failure Indicators

Load test **FAILS** if:

❌ Request rate < 8,000 req/s (< 80% of target)  
❌ P95 response time > 800ms  
❌ Error rate > 2%  
❌ Pods crash or OOM kill  
❌ Database connection exhaustion  
❌ Message queue growing unbounded

## Analyzing Bottlenecks

### Database Bottleneck

**Symptoms**:
- High query duration
- Connection pool exhaustion
- Slow P95/P99 times

**Solutions**:
```sql
-- Check slow queries
SELECT query, mean_exec_time, calls
FROM pg_stat_statements
ORDER BY mean_exec_time DESC
LIMIT 10;

-- Check connection usage
SELECT count(*) FROM pg_stat_activity;
```

**Fixes**:
- Increase connection pool size
- Add database indexes
- Optimize queries with EXPLAIN ANALYZE
- Scale database vertically

### Message Queue Bottleneck

**Symptoms**:
- Growing queue depth
- Delayed webhook delivery
- High consumer lag

**Solutions**:
```bash
# Check queue depth
rabbitmqctl list_queues name messages consumers

# Check consumer performance
rabbitmqctl list_consumers
```

**Fixes**:
- Scale worker pods horizontally
- Optimize consumer processing
- Increase prefetch count
- Add more queue partitions

### API Bottleneck

**Symptoms**:
- High CPU usage in API pods
- Slow response times
- Request timeout errors

**Solutions**:
```bash
# Check pod CPU/memory
kubectl top pods -n hookverse

# Check pod logs for errors
kubectl logs -n hookverse deployment/hookverse-api --tail=100
```

**Fixes**:
- Scale API pods horizontally
- Enable response caching
- Optimize hot code paths
- Review middleware overhead

### Network Bottleneck

**Symptoms**:
- High connection time
- Network timeout errors
- Packet loss

**Solutions**:
```bash
# Check network latency
ping api.hookverse.example.com

# Check DNS resolution
nslookup api.hookverse.example.com

# Check network policies
kubectl get networkpolicies -n hookverse
```

**Fixes**:
- Use connection pooling
- Enable HTTP/2
- Optimize network policies
- Use CDN for static assets

## Optimization Checklist

Before load testing, ensure:

### Infrastructure
- [ ] Sufficient pod replicas deployed
- [ ] Resource limits configured appropriately
- [ ] Horizontal Pod Autoscaler (HPA) enabled
- [ ] Database tuned for high concurrency
- [ ] Message broker optimized
- [ ] Redis cache enabled

### Application
- [ ] Response caching enabled
- [ ] Compiled queries implemented
- [ ] Connection pooling optimized
- [ ] Async processing enabled
- [ ] Batch operations used where applicable
- [ ] Unnecessary logging disabled

### Monitoring
- [ ] Prometheus scraping metrics
- [ ] Grafana dashboards created
- [ ] Alert rules configured
- [ ] Application Insights enabled
- [ ] Log aggregation working

## Load Test Report Template

After running load test, document results:

```markdown
# Load Test Report

**Date**: 2025-10-20  
**Duration**: 30 minutes  
**Target**: 10,000 req/s

## Configuration

- API Pods: 5 replicas (2 CPU, 4GB RAM each)
- Worker Pods: 10 replicas (1 CPU, 2GB RAM each)
- Database: 4 CPU, 8GB RAM
- Redis: Enabled
- Caching: Enabled

## Results

### Performance Metrics

| Metric | Target | Actual | Status |
|--------|--------|--------|--------|
| Request Rate | 10,000 req/s | 10,245 req/s | ✅ PASS |
| P95 Response Time | < 500ms | 342ms | ✅ PASS |
| P99 Response Time | < 1000ms | 687ms | ✅ PASS |
| Error Rate | < 1% | 0.3% | ✅ PASS |

### Resource Usage

| Component | CPU | Memory | Status |
|-----------|-----|--------|--------|
| API Pods | 72% | 68% | ✅ OK |
| Worker Pods | 65% | 54% | ✅ OK |
| Database | 78% | 71% | ✅ OK |
| RabbitMQ | 45% | 52% | ✅ OK |
| Redis | 23% | 31% | ✅ OK |

### Bottlenecks Identified

None. System performed within expected parameters.

### Recommendations

1. Consider scaling to 6 API pods for 20% headroom
2. Monitor database connection pool during peak hours
3. Enable additional caching for event type lookups

## Conclusion

System successfully handles 10,000 webhooks/second with 98% success rate
and P95 response time of 342ms. Load test **PASSED**.
```

## Continuous Load Testing

### Scheduled Tests

Run load tests on schedule:

```yaml
# .github/workflows/load-test.yml
name: Weekly Load Test

on:
  schedule:
    - cron: '0 2 * * 0'  # Sundays at 2 AM
  workflow_dispatch:

jobs:
  load-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup k6
        run: |
          sudo gpg -k
          sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D00
          echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
          sudo apt-get update
          sudo apt-get install k6
      
      - name: Run load test
        env:
          API_URL: ${{ secrets.STAGING_API_URL }}
          API_KEY: ${{ secrets.STAGING_API_KEY }}
        run: |
          k6 run --out json=results.json .specify/scripts/k6/load-test.js
      
      - name: Upload results
        uses: actions/upload-artifact@v3
        with:
          name: load-test-results
          path: results.json
```

### Load Test in CI/CD

Run lighter load test on every deploy:

```yaml
- name: Smoke test
  run: |
    k6 run --stage 1m:100,2m:100,1m:0 .specify/scripts/k6/load-test.js
```

## Troubleshooting

### Issue: k6 errors with "too many open files"

**Solution**: Increase file descriptor limit:

```bash
# Linux
ulimit -n 65535

# Or permanently in /etc/security/limits.conf
* soft nofile 65535
* hard nofile 65535
```

### Issue: Connection refused errors

**Solution**: Check API is running and accessible:

```bash
curl -I http://localhost:5000/health
```

### Issue: High error rate during test

**Solution**: Check logs for specific errors:

```bash
kubectl logs -n hookverse deployment/hookverse-api --tail=1000 | grep ERROR
```

### Issue: Test hangs or times out

**Solution**: Reduce virtual users or check system resources:

```bash
kubectl top nodes
kubectl top pods -n hookverse
```

## Related Documentation

- [Performance Optimization](../performance/README.md)
- [Monitoring](../observability/README.md)
- [Database Optimization](../performance/connection-pooling.md)
- [Kubernetes Deployment](./kubernetes-deployment.md)
