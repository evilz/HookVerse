# OpenTelemetry Observability

This directory contains OpenTelemetry configuration and documentation for HookVerse observability.

## Overview

HookVerse uses OpenTelemetry for:
- **Distributed Tracing**: Track webhook delivery across services
- **Metrics**: Custom business metrics and runtime telemetry
- **Logs**: Structured logging with correlation IDs
- **Context Propagation**: Trace requests across API → Worker → External Services

## Architecture

```
┌─────────────────┐
│  HookVerse API  │──┐
└─────────────────┘  │
                     │  OTLP/gRPC (4317)
┌─────────────────┐  │  OTLP/HTTP (4318)
│ HookVerse Worker│──┼─────────────────────┐
└─────────────────┘  │                     │
                     │              ┌──────▼──────┐
                     └──────────────►    OTel     │
                                    │  Collector  │
                                    └──────┬──────┘
                                           │
                    ┌──────────────────────┼──────────────────────┐
                    │                      │                      │
              ┌─────▼─────┐         ┌─────▼─────┐         ┌─────▼─────┐
              │ Prometheus │         │   Jaeger  │         │    Loki   │
              │  (Metrics) │         │  (Traces) │         │   (Logs)  │
              └────────────┘         └───────────┘         └───────────┘
                    │                      │                      │
                    └──────────────────────┼──────────────────────┘
                                           │
                                    ┌──────▼──────┐
                                    │   Grafana   │
                                    │ (Dashboards)│
                                    └─────────────┘
```

## Components

### 1. Aspire Dashboard (Local Development)

The .NET Aspire Dashboard provides real-time observability for local development without requiring external tools.

**Access**: http://localhost:17191 (automatically starts with AppHost)

**Key Features**:
- **Resources View**: Monitor all services, containers, and projects
- **Console Logs**: Real-time console output from all services
- **Structured Logs**: Filterable, searchable structured logs with log levels
- **Distributed Traces**: End-to-end request tracing across services
- **Metrics**: Live metrics for CPU, memory, request rates, and custom metrics
- **Zero Configuration**: Automatically configured when using ServiceDefaults

**Starting the Dashboard**:
```powershell
# Start all services with Aspire orchestration
dotnet run --project src/HookVerse.AppHost

# Dashboard automatically opens at http://localhost:17191
# Token displayed in console for authentication
```

**Dashboard Views**:

1. **Resources Tab**:
   - Service status (Running/Stopped/Unhealthy)
   - Container health checks
   - Resource endpoints (HTTP, gRPC)
   - Environment variables
   - Start/Stop individual services

2. **Console Logs Tab**:
   - Real-time console output
   - Filter by service name
   - Search across all logs
   - Copy/download logs

3. **Structured Logs Tab**:
   - Formatted JSON logs
   - Filter by log level (Trace, Debug, Info, Warning, Error, Critical)
   - Filter by category (namespace)
   - Search by message content
   - View log properties and metadata

4. **Traces Tab**:
   - Distributed traces with timeline visualization
   - Trace ID correlation
   - Span details (duration, status, tags)
   - Filter by service, operation, status
   - Search by trace ID or operation name

5. **Metrics Tab**:
   - Live charts for system metrics (CPU, Memory, GC)
   - HTTP request metrics (rate, duration, errors)
   - Custom business metrics
   - Configurable time range
   - Export to CSV

**Development Workflow**:
```powershell
# 1. Start services
dotnet run --project src/HookVerse.AppHost

# 2. Open dashboard
# Browser opens automatically to http://localhost:17191

# 3. Make API request
Invoke-RestMethod -Uri http://localhost:7001/api/v1/webhooks -Method Post -Body $payload

# 4. View in dashboard:
#    - Traces tab: See end-to-end trace (API → Worker → External)
#    - Logs tab: Filter to "HookVerse.Api" to see request logs
#    - Metrics tab: Check request rate and latency
```

**Telemetry Collection**:
- Services automatically export to Aspire Dashboard via OTLP
- No environment variables needed for local development
- Dashboard stores telemetry in-memory (lost on restart)
- Dashboard port configurable in AppHost (default: 17191)

**Production Alternative**: Aspire Dashboard is for local development only. Use production-grade tools (Datadog, Application Insights, Grafana) for deployed environments.

### 2. OpenTelemetry Collector (Production)

The OpenTelemetry Collector receives, processes, and exports telemetry data in production environments.

**Key Features**:
- OTLP receivers (gRPC: 4317, HTTP: 4318)
- Prometheus scraping for metrics
- Batch processing for efficiency
- Resource detection (Kubernetes metadata)
- Sampling and filtering
- Multiple exporters (Prometheus, Jaeger, Tempo, Loki)

**Configuration**: [`otel-collector-config.yaml`](./otel-collector-config.yaml)

### 3. Instrumentation

HookVerse services use OpenTelemetry .NET SDK for automatic and manual instrumentation.

**Packages**:
```xml
<PackageReference Include="OpenTelemetry" Version="1.7.0" />
<PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.7.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.7.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.7.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.EntityFrameworkCore" Version="1.0.0-beta.10" />
<PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.7.0" />
<PackageReference Include="OpenTelemetry.Exporter.Prometheus.AspNetCore" Version="1.7.0-alpha.1" />
```

**Configuration** (in `Program.cs`):
```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProvider =>
    {
        tracerProvider
            .AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
                options.EnrichWithHttpRequest = (activity, request) =>
                {
                    activity.SetTag("http.flavor", request.Protocol);
                    activity.SetTag("http.scheme", request.Scheme);
                };
            })
            .AddHttpClientInstrumentation(options =>
            {
                options.RecordException = true;
                options.FilterHttpRequestMessage = (request) =>
                {
                    // Don't trace health checks
                    return !request.RequestUri.AbsolutePath.Contains("/health");
                };
            })
            .AddEntityFrameworkCoreInstrumentation(options =>
            {
                options.SetDbStatementForText = true;
            })
            .AddSource("HookVerse.Api")
            .AddSource("HookVerse.Worker")
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService("hookverse-api", serviceVersion: "1.0.0")
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "production",
                    ["service.namespace"] = "hookverse"
                }))
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(configuration["OpenTelemetry:Endpoint"] ?? "http://otel-collector:4317");
                options.Protocol = OtlpExportProtocol.Grpc;
            });
    })
    .WithMetrics(meterProvider =>
    {
        meterProvider
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddMeter("HookVerse.Api")
            .AddMeter("HookVerse.Worker")
            .AddPrometheusExporter();
    });
```

### 4. Custom Metrics

**Webhook Delivery Metrics**:
```csharp
public class WebhookMetrics
{
    private readonly Counter<long> _webhooksSent;
    private readonly Counter<long> _webhooksDelivered;
    private readonly Counter<long> _webhooksFailed;
    private readonly Histogram<double> _deliveryDuration;
    private readonly UpDownCounter<long> _queueLength;

    public WebhookMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("HookVerse.Worker");
        
        _webhooksSent = meter.CreateCounter<long>(
            "hookverse.webhooks.sent",
            unit: "1",
            description: "Total number of webhooks sent");
        
        _webhooksDelivered = meter.CreateCounter<long>(
            "hookverse.webhooks.delivered",
            unit: "1",
            description: "Total number of webhooks successfully delivered");
        
        _webhooksFailed = meter.CreateCounter<long>(
            "hookverse.webhooks.failed",
            unit: "1",
            description: "Total number of webhooks that failed delivery");
        
        _deliveryDuration = meter.CreateHistogram<double>(
            "hookverse.webhook.delivery_duration",
            unit: "s",
            description: "Webhook delivery duration in seconds");
        
        _queueLength = meter.CreateUpDownCounter<long>(
            "hookverse.webhook.queue_length",
            unit: "1",
            description: "Current webhook queue length");
    }

    public void RecordWebhookSent(string eventType)
    {
        _webhooksSent.Add(1, new KeyValuePair<string, object?>("event.type", eventType));
    }

    public void RecordWebhookDelivered(string eventType, int attemptNumber, double durationSeconds)
    {
        _webhooksDelivered.Add(1, 
            new KeyValuePair<string, object?>("event.type", eventType),
            new KeyValuePair<string, object?>("attempt.number", attemptNumber));
        
        _deliveryDuration.Record(durationSeconds,
            new KeyValuePair<string, object?>("event.type", eventType),
            new KeyValuePair<string, object?>("success", true));
    }

    public void RecordWebhookFailed(string eventType, string errorType, int attemptNumber, double durationSeconds)
    {
        _webhooksFailed.Add(1,
            new KeyValuePair<string, object?>("event.type", eventType),
            new KeyValuePair<string, object?>("error.type", errorType),
            new KeyValuePair<string, object?>("attempt.number", attemptNumber));
        
        _deliveryDuration.Record(durationSeconds,
            new KeyValuePair<string, object?>("event.type", eventType),
            new KeyValuePair<string, object?>("success", false));
    }

    public void UpdateQueueLength(long length)
    {
        _queueLength.Add(length);
    }
}
```

### 4. Custom Tracing

**Activity Sources** for custom spans:
```csharp
public class WebhookDeliveryService
{
    private static readonly ActivitySource ActivitySource = new("HookVerse.Worker");

    public async Task DeliverWebhookAsync(Webhook webhook)
    {
        using var activity = ActivitySource.StartActivity("DeliverWebhook");
        activity?.SetTag("webhook.id", webhook.Id);
        activity?.SetTag("event.type", webhook.EventType);
        activity?.SetTag("subscription.id", webhook.SubscriptionId);
        activity?.SetTag("attempt.number", webhook.AttemptNumber);

        try
        {
            var result = await _httpClient.PostAsync(webhook.Url, webhook.Payload);
            
            activity?.SetTag("http.status_code", (int)result.StatusCode);
            activity?.SetStatus(result.IsSuccessStatusCode 
                ? ActivityStatusCode.Ok 
                : ActivityStatusCode.Error, 
                result.ReasonPhrase);
            
            if (!result.IsSuccessStatusCode)
            {
                activity?.AddEvent(new ActivityEvent("DeliveryFailed", 
                    tags: new ActivityTagsCollection
                    {
                        { "http.status_code", (int)result.StatusCode },
                        { "http.response.body", await result.Content.ReadAsStringAsync() }
                    }));
            }
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }
}
```

## Deployment

### Kubernetes Deployment

Create the OpenTelemetry Collector deployment:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: otel-collector
  namespace: hookverse
spec:
  replicas: 2
  selector:
    matchLabels:
      app: otel-collector
  template:
    metadata:
      labels:
        app: otel-collector
    spec:
      serviceAccountName: otel-collector
      containers:
        - name: otel-collector
          image: otel/opentelemetry-collector-contrib:0.89.0
          args:
            - "--config=/etc/otel-collector-config.yaml"
          ports:
            - containerPort: 4317  # OTLP gRPC
              name: otlp-grpc
            - containerPort: 4318  # OTLP HTTP
              name: otlp-http
            - containerPort: 8889  # Prometheus exporter
              name: prometheus
            - containerPort: 13133 # Health check
              name: health
            - containerPort: 8888  # Metrics
              name: metrics
          volumeMounts:
            - name: config
              mountPath: /etc/otel-collector-config.yaml
              subPath: otel-collector-config.yaml
          env:
            - name: K8S_NODE_NAME
              valueFrom:
                fieldRef:
                  fieldPath: spec.nodeName
            - name: PROMETHEUS_API_KEY
              valueFrom:
                secretKeyRef:
                  name: hookverse-secrets
                  key: prometheus-api-key
          resources:
            requests:
              memory: 256Mi
              cpu: 200m
            limits:
              memory: 512Mi
              cpu: 1000m
          livenessProbe:
            httpGet:
              path: /health
              port: 13133
            initialDelaySeconds: 10
            periodSeconds: 30
          readinessProbe:
            httpGet:
              path: /health
              port: 13133
            initialDelaySeconds: 5
            periodSeconds: 10
      volumes:
        - name: config
          configMap:
            name: otel-collector-config
---
apiVersion: v1
kind: Service
metadata:
  name: otel-collector
  namespace: hookverse
spec:
  selector:
    app: otel-collector
  ports:
    - name: otlp-grpc
      port: 4317
      targetPort: 4317
    - name: otlp-http
      port: 4318
      targetPort: 4318
    - name: prometheus
      port: 8889
      targetPort: 8889
    - name: metrics
      port: 8888
      targetPort: 8888
  type: ClusterIP
---
apiVersion: v1
kind: ServiceAccount
metadata:
  name: otel-collector
  namespace: hookverse
---
apiVersion: rbac.authorization.k8s.io/v1
kind: ClusterRole
metadata:
  name: otel-collector
rules:
  - apiGroups: [""]
    resources: ["pods", "nodes", "services", "endpoints"]
    verbs: ["get", "list", "watch"]
  - apiGroups: ["apps"]
    resources: ["replicasets", "deployments"]
    verbs: ["get", "list", "watch"]
---
apiVersion: rbac.authorization.k8s.io/v1
kind: ClusterRoleBinding
metadata:
  name: otel-collector
roleRef:
  apiGroup: rbac.authorization.k8s.io
  kind: ClusterRole
  name: otel-collector
subjects:
  - kind: ServiceAccount
    name: otel-collector
    namespace: hookverse
```

Create ConfigMap:

```bash
kubectl create configmap otel-collector-config \
  --from-file=otel-collector-config.yaml \
  -n hookverse
```

### Helm Chart Integration

Add to HookVerse Helm chart `values.yaml`:

```yaml
opentelemetry:
  enabled: true
  collector:
    image:
      repository: otel/opentelemetry-collector-contrib
      tag: 0.89.0
    replicas: 2
    resources:
      requests:
        memory: 256Mi
        cpu: 200m
      limits:
        memory: 512Mi
        cpu: 1000m
    config: |
      # Paste otel-collector-config.yaml content here

  # Configure services to send telemetry to collector
  endpoint: "http://otel-collector:4317"
```

## Trace Examples

### Webhook Delivery Trace

A typical trace for webhook delivery:

```
DeliverWebhook [2.3s]
├── ValidateWebhook [10ms]
├── LoadSubscription [25ms]
│   └── Database Query [20ms]
├── PreparePayload [15ms]
│   ├── SerializeEvent [8ms]
│   └── SignPayload [7ms]
└── SendHttpRequest [2.2s]
    ├── DNS Resolution [50ms]
    ├── TLS Handshake [150ms]
    └── HTTP Request [2s]
```

### API Request Trace

```
POST /api/events [150ms]
├── Authentication [20ms]
│   └── ValidateApiKey [18ms]
├── RequestValidation [10ms]
├── SaveEvent [30ms]
│   └── Database Insert [25ms]
├── PublishToQueue [80ms]
│   └── RabbitMQ Publish [75ms]
└── GenerateResponse [10ms]
```

## Queries and Analysis

### Common Trace Queries (Jaeger/Tempo)

```
# Find slow webhook deliveries
service.name="hookverse-worker" AND operation="DeliverWebhook" AND duration > 1s

# Find failed deliveries
service.name="hookverse-worker" AND status=error AND operation="DeliverWebhook"

# Trace specific webhook
webhook.id="123e4567-e89b-12d3-a456-426614174000"

# High latency database queries
db.system="postgresql" AND duration > 500ms
```

### Metrics Queries (Prometheus)

```promql
# Webhook delivery rate by service
rate(hookverse_webhooks_sent_total[5m])

# P95 delivery latency
histogram_quantile(0.95, sum(rate(hookverse_webhook_delivery_duration_seconds_bucket[5m])) by (le))

# Error rate by event type
sum(rate(hookverse_webhooks_failed_total[5m])) by (event_type)
```

## Production OTLP Exporter Configuration

HookVerse services are configured to export telemetry using the OpenTelemetry Protocol (OTLP) when running in production. The OTLP exporter is conditionally enabled based on environment configuration.

### Configuration

The OTLP exporter is enabled by setting the `OTEL_EXPORTER_OTLP_ENDPOINT` environment variable. This is configured in the `appsettings.Production.json` files for each service.

**Conditional Activation**:
```csharp
// In ServiceDefaults/Extensions.cs
private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder) 
    where TBuilder : IHostApplicationBuilder
{
    var useOtlpExporter = !string.IsNullOrWhiteSpace(
        builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

    if (useOtlpExporter)
    {
        builder.Services.AddOpenTelemetry().UseOtlpExporter();
    }
    
    return builder;
}
```

### Datadog Integration

To export telemetry to Datadog, configure the OTLP endpoint to point to the Datadog Agent:

**Using Datadog Agent** (recommended):
```json
{
  "OTEL_EXPORTER_OTLP_ENDPOINT": "http://datadog-agent:4317",
  "OTEL_RESOURCE_ATTRIBUTES": "service.name=hookverse-api,env=production"
}
```

**Direct to Datadog API** (agentless):
```json
{
  "OTEL_EXPORTER_OTLP_ENDPOINT": "https://api.datadoghq.com:4317",
  "OTEL_EXPORTER_OTLP_HEADERS": "dd-api-key=<YOUR_API_KEY>",
  "OTEL_RESOURCE_ATTRIBUTES": "service.name=hookverse-api,env=production"
}
```

**Kubernetes Deployment with Datadog**:
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
          image: hookverse/api:latest
          env:
            - name: OTEL_EXPORTER_OTLP_ENDPOINT
              value: "http://$(DD_AGENT_HOST):4317"
            - name: DD_AGENT_HOST
              valueFrom:
                fieldRef:
                  fieldPath: status.hostIP
            - name: OTEL_RESOURCE_ATTRIBUTES
              value: "service.name=hookverse-api,env=production,service.version=1.0.0"
```

**Datadog Agent Configuration**:
```yaml
# datadog-agent-config.yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: datadog-agent-config
data:
  datadog.yaml: |
    otlp_config:
      receiver:
        protocols:
          grpc:
            endpoint: 0.0.0.0:4317
          http:
            endpoint: 0.0.0.0:4318
      traces:
        span_name_remappings:
          "HTTP GET": "GET"
          "HTTP POST": "POST"
```

### Azure Application Insights Integration

To export telemetry to Azure Application Insights, use the Azure Monitor exporter:

**Using Connection String**:
```json
{
  "APPLICATIONINSIGHTS_CONNECTION_STRING": "InstrumentationKey=<YOUR_KEY>;IngestionEndpoint=https://<REGION>.in.applicationinsights.azure.com/;LiveEndpoint=https://<REGION>.livediagnostics.monitor.azure.com/"
}
```

**Using OTLP Endpoint**:
```json
{
  "OTEL_EXPORTER_OTLP_ENDPOINT": "https://<REGION>.in.applicationinsights.azure.com/v2.1/track",
  "OTEL_EXPORTER_OTLP_HEADERS": "x-api-key=<YOUR_INSTRUMENTATION_KEY>"
}
```

**Azure Kubernetes Service (AKS) with Managed Identity**:
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: hookverse-api
spec:
  template:
    metadata:
      labels:
        aadpodidbinding: hookverse-identity
    spec:
      containers:
        - name: api
          image: hookverse/api:latest
          env:
            - name: APPLICATIONINSIGHTS_CONNECTION_STRING
              valueFrom:
                secretKeyRef:
                  name: appinsights-connection
                  key: connection-string
            - name: AZURE_CLIENT_ID
              value: "<MANAGED_IDENTITY_CLIENT_ID>"
```

**Enable Azure Monitor Exporter** (in ServiceDefaults):
```csharp
// Uncomment in Extensions.cs
if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
    builder.Services.AddOpenTelemetry()
       .UseAzureMonitor();
}
```

### Environment-Specific Configuration

**Development** (local):
```json
{
  "OTEL_EXPORTER_OTLP_ENDPOINT": ""  // Empty = uses Aspire dashboard only
}
```

**Staging**:
```json
{
  "OTEL_EXPORTER_OTLP_ENDPOINT": "http://otel-collector.staging:4317",
  "OTEL_RESOURCE_ATTRIBUTES": "service.name=hookverse-api,env=staging"
}
```

**Production**:
```json
{
  "OTEL_EXPORTER_OTLP_ENDPOINT": "https://otel-collector.production:4317",
  "OTEL_EXPORTER_OTLP_PROTOCOL": "grpc",
  "OTEL_RESOURCE_ATTRIBUTES": "service.name=hookverse-api,env=production,service.version=1.0.0"
}
```

### Testing OTLP Configuration

**Verify Exporter is Active**:
```bash
# Check if OTLP endpoint is configured
kubectl exec -it hookverse-api-xxx -- env | grep OTEL_EXPORTER_OTLP_ENDPOINT

# Check application logs for OTLP initialization
kubectl logs hookverse-api-xxx | grep -i "otlp\|telemetry"
```

**Test with Telemetry Endpoint**:
```bash
# Port forward to local OTLP collector for testing
kubectl port-forward svc/otel-collector 4317:4317

# Set environment variable and run service locally
export OTEL_EXPORTER_OTLP_ENDPOINT="http://localhost:4317"
dotnet run --project src/HookVerse.Api
```

**Validate Data Flow**:
```bash
# Check OTel Collector is receiving data
kubectl logs -l app=otel-collector --tail=50 | grep "TracesExporter"

# Verify data in Datadog
# Go to APM > Services and check for "hookverse-api"

# Verify data in Application Insights
# Go to Application Insights > Transaction search
```

### Performance Considerations

1. **Batching**: OTLP exporter batches telemetry automatically (default 512 spans)
2. **Compression**: gRPC protocol uses compression by default
3. **Timeout**: Default export timeout is 30 seconds
4. **Retry**: Automatic retry with exponential backoff
5. **Resource Limits**: OTLP exporter respects ServiceDefaults memory limits

### Security

**TLS Configuration**:
```json
{
  "OTEL_EXPORTER_OTLP_ENDPOINT": "https://otel-collector:4317",
  "OTEL_EXPORTER_OTLP_CERTIFICATE": "/etc/certs/ca.crt"
}
```

**Authentication Headers**:
```json
{
  "OTEL_EXPORTER_OTLP_HEADERS": "api-key=<SECRET>,tenant-id=<TENANT>"
}
```

**Using Kubernetes Secrets**:
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: otlp-config
type: Opaque
stringData:
  endpoint: "https://api.datadoghq.com:4317"
  headers: "dd-api-key=your-secret-key"
---
apiVersion: apps/v1
kind: Deployment
spec:
  template:
    spec:
      containers:
        - name: api
          env:
            - name: OTEL_EXPORTER_OTLP_ENDPOINT
              valueFrom:
                secretKeyRef:
                  name: otlp-config
                  key: endpoint
            - name: OTEL_EXPORTER_OTLP_HEADERS
              valueFrom:
                secretKeyRef:
                  name: otlp-config
                  key: headers
```

## Troubleshooting

### No Traces Appearing

1. **Check OTel Collector health**:
   ```bash
   kubectl port-forward svc/otel-collector 13133:13133 -n hookverse
   curl http://localhost:13133/health
   ```

2. **Verify service configuration**:
   ```bash
   # Check environment variable
   kubectl exec -it hookverse-api-xxx -n hookverse -- env | grep OTLP
   ```

3. **Check collector logs**:
   ```bash
   kubectl logs -l app=otel-collector -n hookverse --tail=100
   ```

### High Memory Usage

1. **Reduce batch size**:
   ```yaml
   processors:
     batch:
       send_batch_size: 500  # Reduce from 1000
   ```

2. **Enable sampling**:
   ```yaml
   processors:
     probabilistic_sampler:
       sampling_percentage: 10  # Sample 10% instead of 100%
   ```

3. **Increase memory limit**:
   ```yaml
   processors:
     memory_limiter:
       limit_mib: 1024  # Increase from 512
   ```

### Missing Kubernetes Metadata

1. **Verify service account permissions**:
   ```bash
   kubectl auth can-i get pods --as=system:serviceaccount:hookverse:otel-collector
   ```

2. **Check resourcedetection processor**:
   ```yaml
   processors:
     resourcedetection:
       detectors: [env, system, docker, kubernetes]
       kubernetes:
         auth_type: serviceAccount
   ```

## Best Practices

1. **Sampling**: Use probabilistic sampling in high-traffic environments
2. **Batching**: Always batch telemetry data before export
3. **Resource Limits**: Set memory limits to prevent OOM
4. **Security**: Use TLS for production deployments
5. **Cardinality**: Avoid high-cardinality tags (e.g., user IDs, request IDs)
6. **Correlation**: Always propagate trace context across service boundaries
7. **Sensitive Data**: Filter sensitive data in processors (headers, query params)

## Related Documentation

- [Grafana Dashboards](./grafana/README.md)
- [Prometheus Alerts](./prometheus/README.md)
- [Operations Runbook](../operations/runbook.md)
- [OpenTelemetry .NET Documentation](https://opentelemetry.io/docs/instrumentation/net/)
- [OTel Collector Documentation](https://opentelemetry.io/docs/collector/)
