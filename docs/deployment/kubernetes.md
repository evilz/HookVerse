# HookVerse Kubernetes Deployment Guide

This guide covers deploying HookVerse to Kubernetes using both raw manifests and Helm charts.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Quick Start with Helm](#quick-start-with-helm)
- [Manual Deployment](#manual-deployment)
- [Configuration](#configuration)
- [Scaling](#scaling)
- [Monitoring](#monitoring)
- [Troubleshooting](#troubleshooting)

---

## Prerequisites

- Kubernetes cluster 1.28+ (tested on 1.28, 1.29, 1.30)
- kubectl configured to access your cluster
- Helm 3.12+ (for Helm deployment method)
- PostgreSQL database (can be deployed in-cluster or external)
- RabbitMQ/Kafka/SQS message broker (can be deployed in-cluster or external)
- (Optional) Redis for caching and rate limiting

---

## Quick Start with Helm

### Install HookVerse

```bash
# Add HookVerse Helm repository (when published)
helm repo add hookverse https://hookverse.github.io/helm-charts
helm repo update

# Install with default values
helm install hookverse hookverse/hookverse \
  --namespace hookverse \
  --create-namespace

# Or install from local chart
helm install hookverse ./docs/deployment/helm/hookverse \
  --namespace hookverse \
  --create-namespace
```

### Install with Custom Values

Create a `values-prod.yaml` file:

```yaml
replicaCount:
  api: 3
  worker: 2
  dashboard: 2

image:
  repository: hookverse/hookverse
  tag: "1.0.0"

database:
  type: postgresql
  host: postgres.database.svc.cluster.local
  port: 5432
  database: hookverse
  existingSecret: hookverse-db-secret
  secretKeys:
    username: username
    password: password

messagebus:
  transport: RabbitMQ
  host: rabbitmq.messaging.svc.cluster.local
  port: 5672
  existingSecret: hookverse-mq-secret

ingress:
  enabled: true
  className: nginx
  hosts:
    - host: api.hookverse.example.com
      service: api
    - host: dashboard.hookverse.example.com
      service: dashboard
  tls:
    - secretName: hookverse-tls
      hosts:
        - api.hookverse.example.com
        - dashboard.hookverse.example.com

resources:
  api:
    requests:
      cpu: 500m
      memory: 512Mi
    limits:
      cpu: 2000m
      memory: 2Gi
  worker:
    requests:
      cpu: 500m
      memory: 512Mi
    limits:
      cpu: 2000m
      memory: 2Gi
```

Install with custom values:

```bash
helm install hookverse ./docs/deployment/helm/hookverse \
  --namespace hookverse \
  --create-namespace \
  --values values-prod.yaml
```

### Upgrade Deployment

```bash
helm upgrade hookverse ./docs/deployment/helm/hookverse \
  --namespace hookverse \
  --values values-prod.yaml
```

### Uninstall

```bash
helm uninstall hookverse --namespace hookverse
```

---

## Manual Deployment

### 1. Create Namespace

```bash
kubectl create namespace hookverse
```

### 2. Create Secrets

Create database credentials:

```bash
kubectl create secret generic hookverse-db-secret \
  --from-literal=username=hookverse \
  --from-literal=password=your-secure-password \
  --namespace hookverse
```

Create message bus credentials:

```bash
kubectl create secret generic hookverse-mq-secret \
  --from-literal=username=hookverse \
  --from-literal=password=your-mq-password \
  --namespace hookverse
```

Create API key secret:

```bash
kubectl create secret generic hookverse-api-secret \
  --from-literal=api-key=your-api-key \
  --namespace hookverse
```

### 3. Create ConfigMap

```bash
kubectl apply -f - <<EOF
apiVersion: v1
kind: ConfigMap
metadata:
  name: hookverse-config
  namespace: hookverse
data:
  ASPNETCORE_ENVIRONMENT: "Production"
  Database__Type: "PostgreSQL"
  Database__Host: "postgres.database.svc.cluster.local"
  Database__Port: "5432"
  Database__Database: "hookverse"
  MessageBus__Transport: "RabbitMQ"
  MessageBus__Host: "rabbitmq.messaging.svc.cluster.local"
  MessageBus__Port: "5672"
  OpenTelemetry__Enabled: "true"
  OpenTelemetry__OtlpEndpoint: "http://otel-collector:4317"
  DataRetention__ScheduleIntervalHours: "24"
  DataRetention__RunAtMidnightUtc: "true"
  GdprProcessing__PollingIntervalMinutes: "1"
EOF
```

### 4. Deploy PostgreSQL (Optional - for development)

```yaml
# postgres-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: postgres
  namespace: hookverse
spec:
  replicas: 1
  selector:
    matchLabels:
      app: postgres
  template:
    metadata:
      labels:
        app: postgres
    spec:
      containers:
      - name: postgres
        image: postgres:16
        ports:
        - containerPort: 5432
        env:
        - name: POSTGRES_DB
          value: hookverse
        - name: POSTGRES_USER
          valueFrom:
            secretKeyRef:
              name: hookverse-db-secret
              key: username
        - name: POSTGRES_PASSWORD
          valueFrom:
            secretKeyRef:
              name: hookverse-db-secret
              key: password
        volumeMounts:
        - name: postgres-storage
          mountPath: /var/lib/postgresql/data
      volumes:
      - name: postgres-storage
        persistentVolumeClaim:
          claimName: postgres-pvc
---
apiVersion: v1
kind: Service
metadata:
  name: postgres
  namespace: hookverse
spec:
  selector:
    app: postgres
  ports:
  - port: 5432
    targetPort: 5432
```

### 5. Deploy RabbitMQ (Optional - for development)

```yaml
# rabbitmq-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: rabbitmq
  namespace: hookverse
spec:
  replicas: 1
  selector:
    matchLabels:
      app: rabbitmq
  template:
    metadata:
      labels:
        app: rabbitmq
    spec:
      containers:
      - name: rabbitmq
        image: rabbitmq:3.13-management
        ports:
        - containerPort: 5672
        - containerPort: 15672
        env:
        - name: RABBITMQ_DEFAULT_USER
          valueFrom:
            secretKeyRef:
              name: hookverse-mq-secret
              key: username
        - name: RABBITMQ_DEFAULT_PASS
          valueFrom:
            secretKeyRef:
              name: hookverse-mq-secret
              key: password
---
apiVersion: v1
kind: Service
metadata:
  name: rabbitmq
  namespace: hookverse
spec:
  selector:
    app: rabbitmq
  ports:
  - name: amqp
    port: 5672
    targetPort: 5672
  - name: management
    port: 15672
    targetPort: 15672
```

### 6. Deploy HookVerse API

```yaml
# api-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: hookverse-api
  namespace: hookverse
spec:
  replicas: 3
  selector:
    matchLabels:
      app: hookverse-api
  template:
    metadata:
      labels:
        app: hookverse-api
    spec:
      containers:
      - name: api
        image: hookverse/api:latest
        ports:
        - containerPort: 8080
        env:
        - name: Database__Username
          valueFrom:
            secretKeyRef:
              name: hookverse-db-secret
              key: username
        - name: Database__Password
          valueFrom:
            secretKeyRef:
              name: hookverse-db-secret
              key: password
        - name: MessageBus__Username
          valueFrom:
            secretKeyRef:
              name: hookverse-mq-secret
              key: username
        - name: MessageBus__Password
          valueFrom:
            secretKeyRef:
              name: hookverse-mq-secret
              key: password
        envFrom:
        - configMapRef:
            name: hookverse-config
        resources:
          requests:
            cpu: 500m
            memory: 512Mi
          limits:
            cpu: 2000m
            memory: 2Gi
        livenessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /ready
            port: 8080
          initialDelaySeconds: 10
          periodSeconds: 5
---
apiVersion: v1
kind: Service
metadata:
  name: hookverse-api
  namespace: hookverse
spec:
  selector:
    app: hookverse-api
  ports:
  - port: 80
    targetPort: 8080
  type: ClusterIP
```

### 7. Deploy HookVerse Worker

```yaml
# worker-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: hookverse-worker
  namespace: hookverse
spec:
  replicas: 2
  selector:
    matchLabels:
      app: hookverse-worker
  template:
    metadata:
      labels:
        app: hookverse-worker
    spec:
      containers:
      - name: worker
        image: hookverse/worker:latest
        env:
        - name: Database__Username
          valueFrom:
            secretKeyRef:
              name: hookverse-db-secret
              key: username
        - name: Database__Password
          valueFrom:
            secretKeyRef:
              name: hookverse-db-secret
              key: password
        - name: MessageBus__Username
          valueFrom:
            secretKeyRef:
              name: hookverse-mq-secret
              key: username
        - name: MessageBus__Password
          valueFrom:
            secretKeyRef:
              name: hookverse-mq-secret
              key: password
        envFrom:
        - configMapRef:
            name: hookverse-config
        resources:
          requests:
            cpu: 500m
            memory: 512Mi
          limits:
            cpu: 2000m
            memory: 2Gi
```

### 8. Deploy HookVerse Dashboard

```yaml
# dashboard-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: hookverse-dashboard
  namespace: hookverse
spec:
  replicas: 2
  selector:
    matchLabels:
      app: hookverse-dashboard
  template:
    metadata:
      labels:
        app: hookverse-dashboard
    spec:
      containers:
      - name: dashboard
        image: hookverse/dashboard:latest
        ports:
        - containerPort: 8080
        env:
        - name: ApiClient__BaseUrl
          value: "http://hookverse-api"
        - name: ApiClient__ApiKey
          valueFrom:
            secretKeyRef:
              name: hookverse-api-secret
              key: api-key
        envFrom:
        - configMapRef:
            name: hookverse-config
        resources:
          requests:
            cpu: 250m
            memory: 256Mi
          limits:
            cpu: 1000m
            memory: 1Gi
---
apiVersion: v1
kind: Service
metadata:
  name: hookverse-dashboard
  namespace: hookverse
spec:
  selector:
    app: hookverse-dashboard
  ports:
  - port: 80
    targetPort: 8080
  type: ClusterIP
```

### 9. Create Ingress

```yaml
# ingress.yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: hookverse-ingress
  namespace: hookverse
  annotations:
    cert-manager.io/cluster-issuer: "letsencrypt-prod"
    nginx.ingress.kubernetes.io/ssl-redirect: "true"
spec:
  ingressClassName: nginx
  tls:
  - hosts:
    - api.hookverse.example.com
    - dashboard.hookverse.example.com
    secretName: hookverse-tls
  rules:
  - host: api.hookverse.example.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: hookverse-api
            port:
              number: 80
  - host: dashboard.hookverse.example.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: hookverse-dashboard
            port:
              number: 80
```

### 10. Apply All Manifests

```bash
kubectl apply -f postgres-deployment.yaml
kubectl apply -f rabbitmq-deployment.yaml
kubectl apply -f api-deployment.yaml
kubectl apply -f worker-deployment.yaml
kubectl apply -f dashboard-deployment.yaml
kubectl apply -f ingress.yaml
```

---

## Configuration

### Environment Variables

All configuration can be provided via environment variables or ConfigMap:

**Database Configuration**:
- `Database__Type`: Database type (PostgreSQL, SQLite, SQLServer, MySQL)
- `Database__Host`: Database host
- `Database__Port`: Database port
- `Database__Database`: Database name
- `Database__Username`: Database username (from secret)
- `Database__Password`: Database password (from secret)

**Message Bus Configuration**:
- `MessageBus__Transport`: Transport type (RabbitMQ, Kafka, SQS)
- `MessageBus__Host`: Message broker host
- `MessageBus__Port`: Message broker port
- `MessageBus__Username`: Message broker username (from secret)
- `MessageBus__Password`: Message broker password (from secret)

**OpenTelemetry Configuration**:
- `OpenTelemetry__Enabled`: Enable OpenTelemetry (true/false)
- `OpenTelemetry__OtlpEndpoint`: OTLP collector endpoint

**Data Retention Configuration**:
- `DataRetention__ScheduleIntervalHours`: Retention schedule interval (default: 24)
- `DataRetention__RunAtMidnightUtc`: Run at midnight UTC (true/false)

**GDPR Configuration**:
- `GdprProcessing__PollingIntervalMinutes`: GDPR request polling interval (default: 1)

### Database Migration

Run database migrations using a Kubernetes Job:

```yaml
apiVersion: batch/v1
kind: Job
metadata:
  name: hookverse-migrate
  namespace: hookverse
spec:
  template:
    spec:
      containers:
      - name: migrate
        image: hookverse/api:latest
        command: ["dotnet", "ef", "database", "update"]
        env:
        - name: Database__Username
          valueFrom:
            secretKeyRef:
              name: hookverse-db-secret
              key: username
        - name: Database__Password
          valueFrom:
            secretKeyRef:
              name: hookverse-db-secret
              key: password
        envFrom:
        - configMapRef:
            name: hookverse-config
      restartPolicy: OnFailure
```

---

## Scaling

### Horizontal Pod Autoscaling

```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: hookverse-api-hpa
  namespace: hookverse
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: hookverse-api
  minReplicas: 3
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
```

### Manual Scaling

```bash
# Scale API pods
kubectl scale deployment hookverse-api --replicas=5 -n hookverse

# Scale worker pods
kubectl scale deployment hookverse-worker --replicas=3 -n hookverse
```

---

## Monitoring

### Prometheus ServiceMonitor

```yaml
apiVersion: monitoring.coreos.com/v1
kind: ServiceMonitor
metadata:
  name: hookverse-api
  namespace: hookverse
spec:
  selector:
    matchLabels:
      app: hookverse-api
  endpoints:
  - port: http
    path: /metrics
    interval: 30s
```

### Grafana Dashboards

Import pre-built dashboards from `docs/observability/grafana/`:
- `webhook-delivery-dashboard.json`: Webhook delivery metrics
- `mock-endpoint-dashboard.json`: Mock endpoint usage
- `gdpr-compliance-dashboard.json`: GDPR operations

---

## Troubleshooting

### Check Pod Status

```bash
kubectl get pods -n hookverse
kubectl describe pod <pod-name> -n hookverse
```

### View Logs

```bash
# API logs
kubectl logs -f deployment/hookverse-api -n hookverse

# Worker logs
kubectl logs -f deployment/hookverse-worker -n hookverse

# Dashboard logs
kubectl logs -f deployment/hookverse-dashboard -n hookverse
```

### Check Database Connection

```bash
kubectl exec -it deployment/hookverse-api -n hookverse -- \
  dotnet ef database update --dry-run
```

### Common Issues

**Pods CrashLoopBackOff**:
- Check database connectivity
- Verify message bus credentials
- Review pod logs for errors

**Database Migration Failures**:
- Ensure database is accessible
- Check database credentials
- Verify network policies allow database access

**Message Bus Connection Issues**:
- Verify message bus is running
- Check credentials and permissions
- Ensure network policies allow message bus access

---

## Production Checklist

- [ ] Use external managed database (RDS, Cloud SQL, Azure SQL)
- [ ] Use external managed message broker (CloudAMQP, Amazon MQ, Azure Service Bus)
- [ ] Configure SSL/TLS certificates with cert-manager
- [ ] Set up Horizontal Pod Autoscaling
- [ ] Configure resource requests and limits
- [ ] Enable OpenTelemetry with proper collector
- [ ] Set up Prometheus monitoring
- [ ] Import Grafana dashboards
- [ ] Configure backup strategy for database
- [ ] Set up log aggregation (ELK, Loki, etc.)
- [ ] Configure network policies for security
- [ ] Use secrets management (Vault, Sealed Secrets, etc.)
- [ ] Enable RBAC and Pod Security Standards
- [ ] Set up disaster recovery procedures
- [ ] Document incident response procedures

---

## Next Steps

- Review [Helm Chart Values](./helm/hookverse/values.yaml) for all configuration options
- Explore [Observability Setup](../observability/) for monitoring configuration
- Check [Operations Runbook](../operations/runbook.md) for operational procedures
