# HookVerse Helm Chart

This Helm chart deploys HookVerse - an Open-Source Webhooks-as-a-Service Platform to Kubernetes.

## Prerequisites

- Kubernetes 1.24+
- Helm 3.8+
- PostgreSQL database (or other supported database)
- RabbitMQ, Kafka, or AWS SQS (message bus)

## Installation

### Quick Start

```bash
# Add HookVerse Helm repository (if available)
helm repo add hookverse https://charts.hookverse.dev
helm repo update

# Install with default values
helm install hookverse hookverse/hookverse --namespace hookverse --create-namespace
```

### Local Installation

```bash
# Clone the repository
git clone https://github.com/evilz/HookVerse.git
cd HookVerse/docs/deployment/helm

# Install from local chart
helm install hookverse ./hookverse --namespace hookverse --create-namespace
```

### Custom Installation

```bash
# Create a custom values file
cat > custom-values.yaml <<EOF
api:
  replicaCount: 5
  
worker:
  replicaCount: 10
  
database:
  provider: PostgreSQL
  connectionString: "Host=postgres;Database=hookverse;Username=user;Password=pass"
  
messageBus:
  transport: RabbitMQ
  rabbitmq:
    host: rabbitmq.default.svc.cluster.local
    port: 5672
EOF

# Install with custom values
helm install hookverse ./hookverse \
  --namespace hookverse \
  --create-namespace \
  --values custom-values.yaml
```

## Configuration

The following table lists the configurable parameters of the HookVerse chart and their default values.

### Global Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `global.namespace` | Namespace to deploy HookVerse | `hookverse` |
| `global.environment` | Environment name | `production` |
| `global.imageRegistry` | Global container image registry | `docker.io` |
| `global.imagePullSecrets` | Global image pull secrets | `[]` |

### API Service Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `api.enabled` | Enable API service | `true` |
| `api.replicaCount` | Number of API replicas | `3` |
| `api.image.repository` | API image repository | `hookverse/api` |
| `api.image.tag` | API image tag | `latest` |
| `api.service.type` | Kubernetes service type | `LoadBalancer` |
| `api.service.port` | Service port | `80` |
| `api.resources.requests.cpu` | CPU request | `500m` |
| `api.resources.requests.memory` | Memory request | `512Mi` |
| `api.autoscaling.enabled` | Enable HPA | `true` |
| `api.autoscaling.minReplicas` | Minimum replicas | `3` |
| `api.autoscaling.maxReplicas` | Maximum replicas | `20` |

### Worker Service Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `worker.enabled` | Enable Worker service | `true` |
| `worker.replicaCount` | Number of Worker replicas | `5` |
| `worker.image.repository` | Worker image repository | `hookverse/worker` |
| `worker.image.tag` | Worker image tag | `latest` |
| `worker.resources.requests.cpu` | CPU request | `1000m` |
| `worker.resources.requests.memory` | Memory request | `1Gi` |
| `worker.autoscaling.enabled` | Enable HPA | `true` |
| `worker.autoscaling.minReplicas` | Minimum replicas | `5` |
| `worker.autoscaling.maxReplicas` | Maximum replicas | `50` |
| `worker.config.maxConcurrency` | Max concurrent webhooks | `100` |

### Database Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `database.provider` | Database provider | `PostgreSQL` |
| `database.connectionString` | Database connection string | `""` |
| `database.maxRetryCount` | Max retry count | `3` |
| `database.commandTimeout` | Command timeout (seconds) | `30` |

### Message Bus Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `messageBus.transport` | Message bus type | `RabbitMQ` |
| `messageBus.rabbitmq.enabled` | Enable RabbitMQ | `true` |
| `messageBus.rabbitmq.host` | RabbitMQ host | `rabbitmq.hookverse.svc.cluster.local` |
| `messageBus.rabbitmq.port` | RabbitMQ port | `5672` |
| `messageBus.kafka.enabled` | Enable Kafka | `false` |
| `messageBus.sqs.enabled` | Enable AWS SQS | `false` |

### Observability Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `observability.enabled` | Enable observability | `true` |
| `observability.otelCollectorEndpoint` | OpenTelemetry collector endpoint | `http://otel-collector.observability.svc.cluster.local:4317` |
| `observability.tracing.enabled` | Enable distributed tracing | `true` |
| `observability.metrics.enabled` | Enable metrics | `true` |

### Feature Flags

| Parameter | Description | Default |
|-----------|-------------|---------|
| `features.schemaValidation` | Enable schema validation | `true` |
| `features.mockEndpoints` | Enable mock endpoints | `true` |
| `features.gdprCompliance` | Enable GDPR compliance | `true` |
| `features.messageBusIntegration` | Enable message bus | `true` |
| `features.dashboard` | Enable dashboard | `true` |

## Upgrading

```bash
# Upgrade to a new version
helm upgrade hookverse hookverse/hookverse \
  --namespace hookverse \
  --values custom-values.yaml

# Upgrade with specific version
helm upgrade hookverse hookverse/hookverse \
  --namespace hookverse \
  --version 1.1.0
```

## Uninstallation

```bash
# Uninstall the release
helm uninstall hookverse --namespace hookverse

# Optionally delete the namespace
kubectl delete namespace hookverse
```

## Examples

### High Availability Setup

```yaml
api:
  replicaCount: 10
  autoscaling:
    minReplicas: 10
    maxReplicas: 50
  resources:
    requests:
      cpu: 1000m
      memory: 1Gi
    limits:
      cpu: 4000m
      memory: 4Gi

worker:
  replicaCount: 20
  autoscaling:
    minReplicas: 20
    maxReplicas: 100
  resources:
    requests:
      cpu: 2000m
      memory: 2Gi
    limits:
      cpu: 8000m
      memory: 8Gi

podDisruptionBudget:
  enabled: true
  api:
    minAvailable: 7
  worker:
    minAvailable: 15
```

### With Ingress

```yaml
ingress:
  enabled: true
  className: nginx
  annotations:
    cert-manager.io/cluster-issuer: "letsencrypt-prod"
    nginx.ingress.kubernetes.io/rate-limit: "1000"
  hosts:
    - host: api.hookverse.example.com
      paths:
        - path: /
          pathType: Prefix
  tls:
    - secretName: hookverse-tls
      hosts:
        - api.hookverse.example.com
```

### With Redis Caching

```yaml
cache:
  enabled: true
  redis:
    host: redis.hookverse.svc.cluster.local
    port: 6379
    password: "your-redis-password"
    defaultExpirationMinutes: 60
```

## Troubleshooting

### Check Pod Status

```bash
kubectl get pods -n hookverse
kubectl describe pod <pod-name> -n hookverse
kubectl logs <pod-name> -n hookverse
```

### Check Service Status

```bash
kubectl get svc -n hookverse
kubectl describe svc hookverse-api -n hookverse
```

### Check HPA Status

```bash
kubectl get hpa -n hookverse
kubectl describe hpa hookverse-api-hpa -n hookverse
```

### Common Issues

1. **Pods not starting**: Check resource limits and node capacity
2. **Database connection failed**: Verify connection string in secrets
3. **Message bus connection failed**: Check RabbitMQ/Kafka availability
4. **High CPU usage**: Increase replicas or resource limits

## Support

- GitHub Issues: https://github.com/evilz/HookVerse/issues
- Documentation: https://hookverse.dev/docs
- Community: https://discord.gg/hookverse
