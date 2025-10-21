# Aspire Configuration Contracts

**Feature**: 002-aspire-orchestration  
**Date**: 2025-10-21  
**Type**: Infrastructure Configuration Interfaces

## Overview

This feature does not expose new REST APIs. Instead, it defines configuration contracts for Aspire orchestration and generated manifest schemas. These contracts ensure consistency between local development, CI/CD manifest generation, and deployment environments.

---

## Contract 1: AppHost Topology Definition

**File**: `src/HookVerse.AppHost/Program.cs`  
**Purpose**: Defines the service topology and dependencies for all environments

### Interface (Pseudo-code representation)

```csharp
interface IDistributedApplicationBuilder
{
    // Container resources (local development)
    IResourceBuilder<PostgresServerResource> AddPostgres(string name);
    IResourceBuilder<RabbitMQServerResource> AddRabbitMQ(string name);
    IResourceBuilder<RedisResource> AddRedis(string name);

    // Azure managed services (production)
    IResourceBuilder<AzureSqlServerResource> AddAzureSqlServer(string name);
    IResourceBuilder<AzureServiceBusResource> AddAzureServiceBus(string name);
    IResourceBuilder<AzureRedisResource> AddAzureRedis(string name);

    // .NET service projects
    IResourceBuilder<ProjectResource> AddProject<TProject>(string name) where TProject : IProjectMetadata;
}

interface IResourceBuilder<T>
{
    IResourceBuilder<T> WithReference(IResourceBuilder resource);
    IResourceBuilder<T> WithReplicas(int count);
    IResourceBuilder<T> WithCpuLimit(string limit);
    IResourceBuilder<T> WithMemoryLimit(string limit);
    IResourceBuilder<T> WithEnvironment(string key, string value);
}
```

### Concrete Implementation

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Define container resources (local dev)
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin();

var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin();

var redis = builder.AddRedis("redis");

// Define .NET service projects
var api = builder.AddProject<Projects.HookVerse_Api>("api")
    .WithReference(postgres)
    .WithReference(rabbitmq)
    .WithReference(redis)
    .WithReplicas(3)
    .WithCpuLimit("500m")
    .WithMemoryLimit("512Mi");

var worker = builder.AddProject<Projects.HookVerse_Worker>("worker")
    .WithReference(postgres)
    .WithReference(rabbitmq)
    .WithReference(redis)
    .WithReplicas(2)
    .WithCpuLimit("250m")
    .WithMemoryLimit("256Mi");

var dashboard = builder.AddProject<Projects.HookVerse_Dashboard>("dashboard")
    .WithReference(postgres)
    .WithReplicas(1)
    .WithCpuLimit("250m")
    .WithMemoryLimit("256Mi");

builder.Build().Run();
```

### Contract Guarantees

1. **Resource Names**: Stable identifiers for service discovery (`"postgres"`, `"rabbitmq"`, `"redis"`, `"api"`, `"worker"`, `"dashboard"`)
2. **Dependency Graph**: Explicit `.WithReference()` calls capture all dependencies
3. **Resource Limits**: Default moderate limits (500m CPU, 512Mi memory for API; 250m CPU, 256Mi memory for Worker/Dashboard)
4. **Configurability**: Limits can be overridden via environment-specific configuration

### Validation

- ✅ All resource names must be unique (enforced by Aspire at build time)
- ✅ Circular dependencies are detected and rejected (enforced by Aspire)
- ✅ Project references must point to valid .csproj files (enforced by Aspire)

---

## Contract 2: ServiceDefaults Integration

**File**: `src/HookVerse.ServiceDefaults/Extensions.cs`  
**Purpose**: Shared configuration applied to all services

### Interface

```csharp
public static class Extensions
{
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder);
    public static IHealthChecksBuilder AddDefaultHealthChecks(this IHealthChecksBuilder builder, IConfiguration configuration);
}
```

### Implementation

```csharp
public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
{
    // Service discovery
    builder.Services.AddServiceDiscovery();

    // HTTP client configuration with service discovery
    builder.Services.ConfigureHttpClientDefaults(http =>
    {
        http.AddStandardResilienceHandler(); // Retry, circuit breaker
        http.AddServiceDiscovery(); // Resolve service names
    });

    // OpenTelemetry
    builder.Services.AddOpenTelemetry()
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation())
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation());

    // Configure OTLP exporter if endpoint provided
    var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
    if (!string.IsNullOrEmpty(otlpEndpoint))
    {
        builder.Services.AddOpenTelemetry().UseOtlpExporter();
    }

    // Health checks
    builder.Services.AddHealthChecks()
        .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" });

    return builder;
}

public static IHealthChecksBuilder AddDefaultHealthChecks(
    this IHealthChecksBuilder builder,
    IConfiguration configuration)
{
    // Add health checks for dependencies
    var postgresConn = configuration.GetConnectionString("postgres");
    if (!string.IsNullOrEmpty(postgresConn))
        builder.AddNpgSql(postgresConn, tags: new[] { "ready" });

    var rabbitmqConn = configuration.GetConnectionString("rabbitmq");
    if (!string.IsNullOrEmpty(rabbitmqConn))
        builder.AddRabbitMQ(rabbitmqConn, tags: new[] { "ready" });

    var redisConn = configuration.GetConnectionString("redis");
    if (!string.IsNullOrEmpty(redisConn))
        builder.AddRedis(redisConn, tags: new[] { "ready" });

    return builder;
}
```

### Service Integration

Each service (Api, Worker, Dashboard) calls this in their `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add service defaults FIRST (before other services)
builder.AddServiceDefaults();

// Add health checks for dependencies
builder.Services.AddHealthChecks()
    .AddDefaultHealthChecks(builder.Configuration);

// ... rest of service configuration ...

var app = builder.Build();

// Map health check endpoints
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();
```

### Contract Guarantees

1. **Service Discovery**: All HTTP clients automatically resolve service names to URIs
2. **Telemetry**: Distributed tracing (trace IDs), metrics (request rates, latency), logs automatically collected
3. **Health Checks**: `/health/live` (liveness) and `/health/ready` (readiness) endpoints exposed on all services
4. **Resilience**: HTTP clients include retry logic and circuit breakers via `AddStandardResilienceHandler()`

### Validation

- ✅ Service discovery resolves `http://api`, `http://worker` correctly (integration test)
- ✅ Telemetry appears in Aspire dashboard (integration test)
- ✅ Health check endpoints return 200 OK when healthy (integration test)
- ✅ OTLP exporter sends data to configured endpoint when set (integration test)

---

## Contract 3: Kubernetes Manifest Schema

**Generated File**: `aspire/manifests/kubernetes/deployment.yaml`  
**Purpose**: Deterministic, version-controllable Kubernetes manifests

### Schema (YAML)

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: api
  labels:
    app: hookverse-api
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
        - name: ConnectionStrings__postgres
          valueFrom:
            configMapKeyRef:
              name: hookverse-config
              key: postgres-connection
        - name: ConnectionStrings__rabbitmq
          valueFrom:
            configMapKeyRef:
              name: hookverse-config
              key: rabbitmq-connection
        - name: ConnectionStrings__redis
          valueFrom:
            configMapKeyRef:
              name: hookverse-config
              key: redis-connection
        resources:
          limits:
            cpu: "500m"
            memory: "512Mi"
          requests:
            cpu: "250m"
            memory: "256Mi"
        livenessProbe:
          httpGet:
            path: /health/live
            port: 8080
          initialDelaySeconds: 10
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 5
---
apiVersion: v1
kind: Service
metadata:
  name: api
spec:
  selector:
    app: hookverse-api
  ports:
  - protocol: TCP
    port: 80
    targetPort: 8080
---
apiVersion: v1
kind: ConfigMap
metadata:
  name: hookverse-config
data:
  postgres-connection: "Host=postgres;Port=5432;Database=hookverse;Username=hookverse;Password=<SECRET>"
  rabbitmq-connection: "amqp://hookverse:<PASSWORD>@rabbitmq:5672/"
  redis-connection: "redis:6379"
```

### Contract Guarantees

1. **Deterministic Output**: Same AppHost → same YAML (snapshot testable)
2. **Resource Limits**: CPU/memory limits per clarification #5 (moderate defaults, configurable)
3. **Health Probes**: Liveness + readiness probes for all services
4. **Service Discovery**: Kubernetes DNS-based service discovery (`http://api`, `http://worker`)
5. **No Secrets**: Passwords replaced with `<SECRET>` placeholders (per clarification #4, deferred for now)

### Validation

- ✅ `kubectl apply --dry-run=client -f aspire/manifests/kubernetes/` succeeds (schema validation)
- ✅ Snapshot test: Compare generated YAML to committed baseline
- ✅ No secrets in plain text (manual review / linting)

---

## Contract 4: Azure Bicep Template Schema

**Generated File**: `aspire/manifests/azure/main.bicep`  
**Purpose**: Infrastructure-as-code for Azure deployment

### Schema (Bicep)

```bicep
@description('Location for all resources')
param location string = resourceGroup().location

@description('Environment name (dev, staging, prod)')
param environmentName string

// Key Vault for secrets
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: 'kv-hookverse-${environmentName}'
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
  }
}

// Azure SQL Database
resource sqlServer 'Microsoft.Sql/servers@2023-05-01-preview' = {
  name: 'sql-hookverse-${environmentName}'
  location: location
  properties: {
    administratorLogin: 'hookverseadmin'
    administratorLoginPassword: '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=sql-admin-password)'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-05-01-preview' = {
  parent: sqlServer
  name: 'hookverse'
  location: location
  sku: {
    name: 'S1'
    tier: 'Standard'
  }
}

// Azure Service Bus
resource serviceBus 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: 'sb-hookverse-${environmentName}'
  location: location
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
}

// Azure Redis Cache
resource redisCache 'Microsoft.Cache/redis@2023-08-01' = {
  name: 'redis-hookverse-${environmentName}'
  location: location
  properties: {
    sku: {
      name: 'Basic'
      family: 'C'
      capacity: 1
    }
  }
}

// Container Apps Environment
resource containerAppEnv 'Microsoft.App/managedEnvironments@2023-05-01' = {
  name: 'env-hookverse-${environmentName}'
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
    }
  }
}

// API Container App
resource apiContainerApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'ca-hookverse-api-${environmentName}'
  location: location
  properties: {
    managedEnvironmentId: containerAppEnv.id
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
      }
      secrets: [
        {
          name: 'postgres-connection'
          keyVaultUrl: '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=postgres-connection)'
        }
        {
          name: 'rabbitmq-connection'
          keyVaultUrl: '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=rabbitmq-connection)'
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'api'
          image: 'hookverse/api:latest'
          resources: {
            cpu: '0.5'
            memory: '512Mi'
          }
          env: [
            {
              name: 'ConnectionStrings__postgres'
              secretRef: 'postgres-connection'
            }
            {
              name: 'ConnectionStrings__rabbitmq'
              secretRef: 'rabbitmq-connection'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 10
      }
    }
  }
}

// Outputs for CI/CD
output apiUrl string = 'https://${apiContainerApp.properties.configuration.ingress.fqdn}'
output keyVaultName string = keyVault.name
```

### Contract Guarantees

1. **Managed Services**: Azure SQL, Service Bus, Redis Cache (per clarification #1)
2. **Key Vault Integration**: Secrets stored in Key Vault, referenced via `@Microsoft.KeyVault(...)` (per clarification #3)
3. **Container Apps**: Serverless container hosting with auto-scaling
4. **Resource Limits**: 0.5 CPU, 512Mi memory for API (per clarification #5)
5. **Outputs**: CI/CD-friendly outputs (API URL, Key Vault name)

### Validation

- ✅ `az bicep build --file aspire/manifests/azure/main.bicep` succeeds (syntax validation)
- ✅ Snapshot test: Compare generated Bicep to committed baseline
- ✅ No plain-text secrets (manual review / linting)

---

## Contract 5: CI/CD Integration

**Purpose**: Replace existing deployment steps with Aspire manifest generation

### Before (Existing Pipeline)

```yaml
# .github/workflows/deploy.yml (OLD)
- name: Build Docker images
  run: docker build -t hookverse/api .

- name: Deploy to Kubernetes
  run: kubectl apply -f k8s/
```

### After (Aspire-Based Pipeline)

```yaml
# .github/workflows/deploy.yml (NEW)
- name: Generate Kubernetes manifests
  run: |
    dotnet publish src/HookVerse.AppHost/HookVerse.AppHost.csproj \
      --output aspire/manifests/kubernetes \
      /p:PublishProfile=kubernetes

- name: Deploy to Kubernetes
  run: kubectl apply -f aspire/manifests/kubernetes/

# OR for Azure:
- name: Generate Azure Bicep
  run: |
    dotnet publish src/HookVerse.AppHost/HookVerse.AppHost.csproj \
      --output aspire/manifests/azure \
      /p:PublishProfile=azure

- name: Deploy to Azure
  run: |
    az deployment group create \
      --resource-group hookverse-prod \
      --template-file aspire/manifests/azure/main.bicep \
      --parameters @aspire/manifests/azure/parameters.json
```

### Contract Guarantees

1. **Single Command**: `dotnet publish` generates all manifests
2. **Deterministic**: Same commit → same manifests (git diff detects changes)
3. **No Manual Edits**: Developers never edit YAML/Bicep directly (edit AppHost instead)
4. **CI/CD Integration**: Per clarification #7, replace existing deployment steps

### Validation

- ✅ CI/CD pipeline runs successfully with new steps
- ✅ Generated manifests match committed baseline (detect drift)
- ✅ Deployment succeeds in test environment

---

## Summary

This feature defines **5 configuration contracts**:

1. **AppHost Topology**: Service topology in `Program.cs` (fluent API)
2. **ServiceDefaults**: Shared configuration extension methods
3. **Kubernetes Manifests**: Generated YAML (Deployment, Service, ConfigMap)
4. **Azure Bicep**: Generated IaC (Container Apps, SQL, Service Bus, Redis, Key Vault)
5. **CI/CD Integration**: Manifest generation + deployment commands

**No REST API contracts** are added (infrastructure feature).

All contracts support **Test-First Development** (Constitution Principle V):
- Unit tests validate AppHost configuration
- Integration tests validate service discovery and health checks
- Schema validation tests validate generated manifests
- Snapshot tests detect unintended manifest changes
