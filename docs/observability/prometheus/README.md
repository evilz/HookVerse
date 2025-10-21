# Prometheus Alert Rules

This directory contains Prometheus alerting rules for HookVerse monitoring.

## Overview

The alert rules are organized into the following groups:

1. **hookverse_webhook_delivery** - Alerts for webhook delivery performance and health
2. **hookverse_api_service** - Alerts for API service availability and performance
3. **hookverse_worker_service** - Alerts for Worker service availability
4. **hookverse_resources** - Alerts for resource usage (CPU, memory, GC)
5. **hookverse_database** - Alerts for database connectivity and performance
6. **hookverse_message_bus** - Alerts for message bus connectivity and consumer lag

## Alert Severity Levels

- **critical**: Requires immediate action; service degradation or outage
- **warning**: Requires attention; potential issues or performance degradation
- **info**: Informational; no immediate action required

## Key Alerts

### Webhook Delivery Alerts

| Alert | Severity | Threshold | Description |
|-------|----------|-----------|-------------|
| WebhookDeliverySuccessRateLow | warning | <95% for 5m | Delivery success rate below 95% |
| WebhookDeliverySuccessRateCritical | critical | <90% for 2m | Delivery success rate critically low |
| WebhookQueueHigh | warning | >10,000 for 10m | Queue length exceeds 10,000 items |
| WebhookQueueCritical | critical | >50,000 for 5m | Queue length exceeds 50,000 items |
| WebhookDeliveryLatencyHigh | warning | p95 >500ms for 10m | Delivery latency high |
| WebhookFailureRateHigh | warning | >5% for 5m | Failure rate exceeds 5% |
| WebhookRetriesExceeded | warning | >10/sec for 5m | High number reaching max retries |

### API Service Alerts

| Alert | Severity | Threshold | Description |
|-------|----------|-----------|-------------|
| APIServiceDown | critical | down for 1m | API service is not responding |
| APIHighErrorRate | warning | 5xx >5% for 5m | High rate of server errors |
| APILatencyHigh | warning | p95 >200ms for 10m | API response time high |
| APILatencyCritical | critical | p95 >1s for 5m | API response time critical |
| APITooFewInstances | warning | <2 for 5m | Insufficient instances for HA |

### Worker Service Alerts

| Alert | Severity | Threshold | Description |
|-------|----------|-----------|-------------|
| WorkerServiceDown | critical | down for 1m | Worker service is not responding |
| WorkerTooFewInstances | warning | <3 for 5m | Insufficient worker instances |

### Resource Alerts

| Alert | Severity | Threshold | Description |
|-------|----------|-----------|-------------|
| HighCPUUsage | warning | >85% for 10m | CPU usage high |
| CriticalCPUUsage | critical | >95% for 5m | CPU usage critical |
| HighMemoryUsage | warning | >80% for 10m | Memory usage high |
| CriticalMemoryUsage | critical | >90% for 5m | Memory usage critical |
| FrequentGarbageCollection | warning | Gen2 >5/sec for 10m | Frequent Gen2 GC collections |

### Database Alerts

| Alert | Severity | Threshold | Description |
|-------|----------|-----------|-------------|
| DatabaseConnectionFailures | critical | >1/sec for 5m | Database connection failures |
| SlowDatabaseQueries | warning | p95 >1s for 10m | Slow database query performance |

### Message Bus Alerts

| Alert | Severity | Threshold | Description |
|-------|----------|-----------|-------------|
| MessageBusConnectionDown | critical | down for 2m | Message bus connection lost |
| MessageBusConsumerLag | warning | >10,000 for 10m | Consumer lag exceeds threshold |

## Installation

### Option 1: Prometheus Configuration File

Add the alert rules to your Prometheus configuration:

```yaml
# prometheus.yml
rule_files:
  - /etc/prometheus/alerts/hookverse-alerts.yml
```

Copy the `alerts.yml` file:

```bash
kubectl create configmap prometheus-alerts \
  --from-file=hookverse-alerts.yml=alerts.yml \
  -n monitoring

# Mount in Prometheus deployment
kubectl patch deployment prometheus \
  -n monitoring \
  --patch '
spec:
  template:
    spec:
      volumes:
      - name: alert-rules
        configMap:
          name: prometheus-alerts
      containers:
      - name: prometheus
        volumeMounts:
        - name: alert-rules
          mountPath: /etc/prometheus/alerts
'
```

### Option 2: Prometheus Operator

Create a PrometheusRule resource:

```bash
kubectl apply -f - <<EOF
apiVersion: monitoring.coreos.com/v1
kind: PrometheusRule
metadata:
  name: hookverse-alerts
  namespace: monitoring
  labels:
    prometheus: kube-prometheus
    role: alert-rules
spec:
  groups:
$(cat alerts.yml | sed 's/^/    /')
EOF
```

### Option 3: Helm Chart Values

If using the HookVerse Helm chart with Prometheus operator:

```yaml
# values.yaml
prometheus:
  enabled: true
  alertRules:
    enabled: true
    customRules:
      - name: hookverse-alerts
        rules: |
$(cat alerts.yml | sed 's/^/          /')
```

## Configuration

### Alert Manager Integration

Configure Alertmanager routes for HookVerse alerts:

```yaml
# alertmanager.yml
route:
  receiver: 'default'
  group_by: ['alertname', 'component']
  group_wait: 30s
  group_interval: 5m
  repeat_interval: 4h
  routes:
    - match:
        severity: critical
      receiver: 'pagerduty-critical'
      continue: true
    
    - match:
        severity: warning
      receiver: 'slack-warnings'
      group_wait: 5m
      group_interval: 10m

receivers:
  - name: 'pagerduty-critical'
    pagerduty_configs:
      - service_key: '<your-pagerduty-key>'
        description: '{{ .GroupLabels.alertname }}: {{ .Annotations.summary }}'
  
  - name: 'slack-warnings'
    slack_configs:
      - api_url: '<your-slack-webhook-url>'
        channel: '#hookverse-alerts'
        title: '{{ .GroupLabels.alertname }}'
        text: '{{ .Annotations.description }}'
```

### Customizing Thresholds

Edit the alert expressions in `alerts.yml` to adjust thresholds:

```yaml
# Example: Change success rate threshold from 95% to 98%
- alert: WebhookDeliverySuccessRateLow
  expr: |
    (
      sum(rate(hookverse_webhooks_delivered_total[5m]))
      /
      sum(rate(hookverse_webhooks_sent_total[5m]))
    ) * 100 < 98  # Changed from 95
  for: 5m
```

### Runbook URLs

Update the runbook URLs in annotations to point to your documentation:

```yaml
annotations:
  runbook: "https://docs.yourcompany.com/runbooks/low-delivery-rate"
```

## Testing Alerts

### Manual Testing

Trigger test alerts using `amtool`:

```bash
# Test webhook delivery alert
amtool alert add \
  WebhookDeliverySuccessRateLow \
  severity=warning \
  component=webhook-delivery \
  --annotation=summary="Test alert"

# View active alerts
amtool alert query
```

### Load Testing

Generate load to trigger performance alerts:

```bash
# Use k6 or similar tool
k6 run --vus 100 --duration 5m load-test.js
```

### Validation

Check that alerts are loaded:

```bash
# Query Prometheus API
curl http://prometheus:9090/api/v1/rules | jq '.data.groups[] | select(.name=="hookverse_webhook_delivery")'

# Check firing alerts
curl http://prometheus:9090/api/v1/alerts | jq '.data.alerts[] | select(.labels.component=="webhook-delivery")'
```

## Troubleshooting

### Alert Not Firing

1. **Check metric availability**:
   ```promql
   # Verify metrics exist
   hookverse_webhooks_sent_total
   hookverse_webhooks_delivered_total
   ```

2. **Check alert expression**:
   ```bash
   # Test expression in Prometheus UI
   # Navigate to: http://prometheus:9090/graph
   # Paste alert expression and check results
   ```

3. **Check evaluation interval**:
   ```yaml
   # Ensure evaluation interval is appropriate
   groups:
     - name: hookverse_webhook_delivery
       interval: 30s  # Adjust if needed
   ```

### Alert Flapping

If alerts are firing and resolving repeatedly:

1. **Increase for duration**:
   ```yaml
   for: 10m  # Increase from 5m to reduce flapping
   ```

2. **Adjust thresholds**:
   ```yaml
   expr: ... > 95  # Make threshold less sensitive
   ```

3. **Use recording rules** for smoother metrics:
   ```yaml
   # Add to recording rules
   - record: hookverse:delivery_success_rate:5m
     expr: |
       sum(rate(hookverse_webhooks_delivered_total[5m]))
       /
       sum(rate(hookverse_webhooks_sent_total[5m])) * 100
   
   # Use in alert
   - alert: WebhookDeliverySuccessRateLow
     expr: hookverse:delivery_success_rate:5m < 95
   ```

### No Alertmanager Notifications

1. **Check Alertmanager connectivity**:
   ```bash
   kubectl port-forward svc/alertmanager 9093:9093 -n monitoring
   curl http://localhost:9093/-/healthy
   ```

2. **Verify alert routing**:
   ```bash
   amtool config routes show
   ```

3. **Check receiver configuration**:
   ```bash
   amtool config show
   ```

## Best Practices

1. **Alert Fatigue**: Only alert on actionable conditions
2. **Severity Levels**: Use critical sparingly for true emergencies
3. **For Duration**: Allow time for transient issues to resolve
4. **Runbooks**: Always link to documentation with resolution steps
5. **Testing**: Regularly test alert rules and notification channels
6. **Review**: Periodically review and adjust thresholds based on actual performance

## Related Documentation

- [Grafana Dashboards](../grafana/README.md)
- [OpenTelemetry Configuration](./README.md)
- [Prometheus Recording Rules](./recording-rules.yml)
- [Runbooks](https://docs.hookverse.dev/runbooks/)
- [Monitoring Guide](../../operations/monitoring.md)
