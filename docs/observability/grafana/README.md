# HookVerse Grafana Dashboards

This directory contains Grafana dashboard JSON definitions for monitoring HookVerse webhook delivery platform.

## Available Dashboards

### 1. Webhook Delivery Dashboard (`webhook-delivery.json`)

**Purpose**: Monitor webhook sending, delivery, and failure rates with detailed metrics

**Key Panels**:
- **Webhook Throughput**: Requests per second for sent, delivered, and failed webhooks
- **Delivery Success Rate**: Percentage gauge showing overall delivery success
- **Active Subscriptions**: Count of active webhook subscriptions
- **Queue Length**: Current webhook delivery queue depth
- **Delivery Latency**: p50, p95, p99 latency percentiles
- **Webhooks by Event Type**: Breakdown of webhook traffic by event type
- **Delivery Attempts Distribution**: Histogram showing retry attempts
- **Failures by Error Type**: Categorized failure rates

**Metrics Used**:
- `hookverse_webhooks_sent_total`
- `hookverse_webhooks_delivered_total`
- `hookverse_webhooks_failed_total`
- `hookverse_webhook_delivery_duration_seconds`
- `hookverse_active_subscriptions`
- `hookverse_webhook_queue_length`
- `hookverse_delivery_attempts`

**Refresh Rate**: 30 seconds

---

### 2. System Health Dashboard (`system-health.json`)

**Purpose**: Monitor overall system health, resource usage, and performance

**Key Panels**:
- **Service Status**: UP/DOWN indicators for API and Worker services
- **Pod Count**: Number of running pods per service
- **CPU Usage**: Overall and per-pod CPU utilization
- **Memory Usage**: Overall and per-pod memory consumption
- **API Response Time**: p50, p95, p99 latency for HTTP requests
- **API Request Rate**: Requests per second by status code (2xx, 4xx, 5xx)
- **.NET Memory**: Managed memory usage tracking
- **.NET GC Collections**: Garbage collection frequency by generation

**Metrics Used**:
- `up` - Service availability
- `process_cpu_seconds_total` - CPU usage
- `process_resident_memory_bytes` - Memory usage
- `http_request_duration_seconds` - API latency
- `http_requests_total` - HTTP request counts
- `dotnet_total_memory_bytes` - .NET memory
- `dotnet_collection_count_total` - GC metrics

**Refresh Rate**: 30 seconds

---

## Installation

### Import via Grafana UI

1. Open Grafana
2. Navigate to **Dashboards** → **Import**
3. Click **Upload JSON file**
4. Select the dashboard JSON file
5. Configure data source (select your Prometheus instance)
6. Click **Import**

### Import via API

```bash
# Import webhook delivery dashboard
curl -X POST \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $GRAFANA_API_KEY" \
  -d @webhook-delivery.json \
  https://grafana.example.com/api/dashboards/db

# Import system health dashboard
curl -X POST \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $GRAFANA_API_KEY" \
  -d @system-health.json \
  https://grafana.example.com/api/dashboards/db
```

### Import via Provisioning

Create a provisioning file at `/etc/grafana/provisioning/dashboards/hookverse.yaml`:

```yaml
apiVersion: 1

providers:
  - name: 'HookVerse'
    orgId: 1
    folder: 'HookVerse'
    type: file
    disableDeletion: false
    updateIntervalSeconds: 10
    allowUiUpdates: true
    options:
      path: /var/lib/grafana/dashboards/hookverse
```

Copy dashboard files to the provisioning directory:

```bash
mkdir -p /var/lib/grafana/dashboards/hookverse
cp webhook-delivery.json /var/lib/grafana/dashboards/hookverse/
cp system-health.json /var/lib/grafana/dashboards/hookverse/
```

---

## Configuration

### Data Source

Both dashboards expect a Prometheus data source named **"Prometheus"**. Update the data source name in the JSON files if yours differs:

```json
{
  "datasource": "YourPrometheusName"
}
```

### Variables

#### Webhook Delivery Dashboard

- **$pod**: Multi-select variable to filter by specific pods (auto-populated from metrics)

### Time Range

Default time range: **Last 1 hour**

Recommended ranges:
- Real-time monitoring: Last 15 minutes
- Troubleshooting: Last 1-6 hours
- Capacity planning: Last 7-30 days

---

## Alerts

These dashboards include visual thresholds but do not create Prometheus alerts. For alerting, see `../prometheus/alerts.yml`.

**Recommended Alert Thresholds**:
- Delivery success rate < 95%
- API p95 latency > 200ms
- Webhook queue length > 10,000
- CPU usage > 85%
- Memory usage > 90%
- Error rate > 5%

---

## Customization

### Adding Panels

1. Open the dashboard in Grafana
2. Click **Add panel** → **Add an empty panel**
3. Configure your query and visualization
4. Save the dashboard
5. Export JSON: **Dashboard settings** → **JSON Model** → Copy JSON

### Modifying Queries

Example: Change webhook delivery latency percentiles

```promql
# Current: p50, p95, p99
histogram_quantile(0.50, sum(rate(hookverse_webhook_delivery_duration_seconds_bucket[5m])) by (le))

# Add p75
histogram_quantile(0.75, sum(rate(hookverse_webhook_delivery_duration_seconds_bucket[5m])) by (le))
```

### Changing Refresh Rate

Edit the JSON `refresh` field:

```json
{
  "refresh": "10s"  // Options: "5s", "10s", "30s", "1m", "5m", "15m"
}
```

---

## Troubleshooting

### No Data Displayed

1. **Check Prometheus data source**:
   - Verify Prometheus is reachable from Grafana
   - Test connection in **Configuration** → **Data Sources**

2. **Verify metrics exist**:
   ```bash
   # Query Prometheus directly
   curl http://prometheus:9090/api/v1/query?query=up{job="hookverse-api"}
   ```

3. **Check metric names**:
   - Ensure application is exporting metrics
   - Verify metric name spelling
   - Check label filters match your deployment

### Panels Show "No data"

- Adjust time range (metrics may not exist in selected period)
- Check query syntax in panel editor
- Verify label matchers (e.g., `job="hookverse-api"`)

### Slow Dashboard Loading

- Reduce time range
- Increase refresh interval
- Optimize queries (use recording rules)
- Enable query caching in Grafana

---

## Best Practices

1. **Use folders**: Organize dashboards in a "HookVerse" folder
2. **Version control**: Keep dashboard JSON in git
3. **Template variables**: Use variables for dynamic filtering
4. **Recording rules**: Pre-compute expensive queries in Prometheus
5. **Annotations**: Add deployment annotations to correlate changes
6. **Links**: Add links between related dashboards
7. **Documentation**: Add panel descriptions for clarity

---

## Recording Rules

For better dashboard performance, create Prometheus recording rules:

```yaml
# /etc/prometheus/rules/hookverse.yml
groups:
  - name: hookverse_rules
    interval: 30s
    rules:
      - record: hookverse:webhook_success_rate:5m
        expr: |
          (
            sum(rate(hookverse_webhooks_delivered_total[5m]))
            /
            sum(rate(hookverse_webhooks_sent_total[5m]))
          ) * 100
      
      - record: hookverse:api_latency_p95:5m
        expr: |
          histogram_quantile(0.95,
            sum(rate(http_request_duration_seconds_bucket{job="hookverse-api"}[5m])) by (le)
          )
```

Update dashboard queries to use recording rules:

```promql
# Instead of:
(sum(rate(hookverse_webhooks_delivered_total[5m])) / sum(rate(hookverse_webhooks_sent_total[5m]))) * 100

# Use:
hookverse:webhook_success_rate:5m
```

---

## Support

- **Documentation**: https://hookverse.dev/docs/observability
- **Issues**: https://github.com/evilz/HookVerse/issues
- **Grafana Docs**: https://grafana.com/docs/grafana/latest/dashboards/
