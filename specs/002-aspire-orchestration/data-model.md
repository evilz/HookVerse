# Data Model: Aspire Orchestration

**Feature**: 002-aspire-orchestration  
**Date**: 2025-10-21

## Overview

This feature introduces infrastructure and configuration entities for Aspire orchestration. No new domain entities for the webhook delivery business logic are added. Instead, this document captures the configuration structures and resource definitions that enable Aspire-based orchestration.

---

## Configuration Entities

### 1. AspireResourceDefinition

**Purpose**: Represents a service, container, or managed resource in the Aspire topology.

**Attributes**:
- `Name` (string, required): Logical name used for service discovery (e.g., "api", "worker", "postgres")
- `Type` (enum, required): ResourceType { Project, Container, Azure SQL, Azure ServiceBus, AzureRedis }
- `ConnectionStringName` (string, optional): Configuration key for injected connection string (e.g., "ConnectionStrings:postgres")
- `Replicas` (int, default 1): Number of instances for horizontal scaling
- `CpuLimit` (string, optional): CPU limit in Kubernetes format (e.g., "500m")
- `MemoryLimit` (string, optional): Memory limit in Kubernetes format (e.g., "512Mi")
- `HealthCheckPath` (string, optional): HTTP path for health probes (e.g., "/health/ready")

**Relationships**:
- One AspireResourceDefinition has many `ServiceDependencies` (which resources it depends on)

**Validation Rules**:
- Name must be unique within the AppHost topology
- CpuLimit must match regex `^\d+m$` (e.g., "100m", "1000m")
- MemoryLimit must match regex `^\d+(Mi|Gi)$` (e.g., "256Mi", "2Gi")
- Replicas must be >= 1

**State Transitions**: N/A (configuration, not runtime state)

**Example**:
```csharp
// In AppHost/Program.cs
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin();

var api = builder.AddProject<HookVerse_Api>("api")
    .WithReference(postgres)
    .WithReplicas(3)
    .WithCpuLimit("500m")
    .WithMemoryLimit("512Mi");
```

---

### 2. ServiceDependency

**Purpose**: Captures dependency relationships between services and resources (e.g., API depends on PostgreSQL, Worker depends on RabbitMQ).

**Attributes**:
- `SourceService` (string, required): Name of the dependent service (e.g., "api")
- `TargetResource` (string, required): Name of the dependency (e.g., "postgres", "rabbitmq")
- `DependencyType` (enum, required): DependencyType { Database, MessageBroker, Cache, ServiceReference }

**Relationships**:
- Many ServiceDependencies belong to one AspireResourceDefinition (SourceService)
- Each ServiceDependency references one AspireResourceDefinition (TargetResource)

**Validation Rules**:
- SourceService and TargetResource must reference existing AspireResourceDefinition names
- Circular dependencies not allowed (enforced at build time by Aspire)

**State Transitions**: N/A (configuration, not runtime state)

**Example**:
```csharp
// API depends on PostgreSQL
builder.AddProject<HookVerse_Api>("api")
    .WithReference(postgres); // Creates ServiceDependency: api -> postgres

// Worker depends on RabbitMQ
builder.AddProject<HookVerse_Worker>("worker")
    .WithReference(rabbitmq); // Creates ServiceDependency: worker -> rabbitmq
```

---

### 3. ManifestOutputConfiguration

**Purpose**: Configuration for generating deployment manifests (Kubernetes YAML, Azure Bicep).

**Attributes**:
- `TargetPlatform` (enum, required): DeploymentTarget { Kubernetes, Azure, DockerCompose }
- `OutputPath` (string, required): Directory path for generated manifests (e.g., "aspire/manifests/kubernetes")
- `IncludeSecrets` (bool, default false): Whether to include Kubernetes Secrets (should be false for security)
- `ResourceLimitsEnabled` (bool, default true): Whether to include CPU/memory limits
- `NamespacePrefix` (string, optional): Kubernetes namespace prefix (e.g., "hookverse-")
- `AzureRegion` (string, optional): Azure region for Bicep deployment (e.g., "eastus")

**Relationships**: N/A (standalone configuration)

**Validation Rules**:
- OutputPath must be a valid directory path
- IncludeSecrets should always be false for production (enforced by policy)
- AzureRegion required when TargetPlatform = Azure

**State Transitions**: N/A (configuration, not runtime state)

**Example**:
```bash
# Kubernetes manifest generation
dotnet publish src/HookVerse.AppHost \
  --output aspire/manifests/kubernetes \
  /p:PublishProfile=kubernetes

# Azure Bicep generation
dotnet publish src/HookVerse.AppHost \
  --output aspire/manifests/azure \
  /p:PublishProfile=azure \
  /p:AzureRegion=eastus
```

---

### 4. ServiceDefaults Configuration

**Purpose**: Shared configuration applied to all services for service discovery, health checks, and observability.

**Attributes**:
- `ServiceDiscoveryEnabled` (bool, default true): Enable Aspire service discovery
- `OpenTelemetryEnabled` (bool, default true): Enable OpenTelemetry instrumentation
- `HealthChecksEnabled` (bool, default true): Enable health check endpoints
- `OtlpExporterEndpoint` (string, optional): OTLP endpoint for telemetry export (e.g., "https://otlp.datadoghq.com")
- `LivenessPath` (string, default "/health/live"): HTTP path for liveness probe
- `ReadinessPath` (string, default "/health/ready"): HTTP path for readiness probe

**Relationships**: Applied to all services via `builder.AddServiceDefaults()` call

**Validation Rules**:
- OtlpExporterEndpoint must be a valid HTTPS URL if provided
- LivenessPath and ReadinessPath must start with "/"

**State Transitions**: N/A (configuration, not runtime state)

**Example**:
```csharp
// In ServiceDefaults/Extensions.cs
public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
{
    // Service discovery
    if (builder.Configuration.GetValue("ServiceDiscoveryEnabled", true))
        builder.Services.AddServiceDiscovery();

    // OpenTelemetry
    if (builder.Configuration.GetValue("OpenTelemetryEnabled", true))
    {
        builder.Services.AddOpenTelemetry()
            .WithTracing(...)
            .WithMetrics(...);
    }

    // Health checks
    if (builder.Configuration.GetValue("HealthChecksEnabled", true))
    {
        builder.Services.AddHealthChecks();
    }

    return builder;
}
```

---

## Existing Domain Entities (No Changes)

The following existing HookVerse domain entities remain unchanged. They are documented here for reference to show that Aspire integration does NOT modify the webhook delivery domain model:

- **Webhook**: Represents a registered webhook subscription
- **WebhookDelivery**: Represents a delivery attempt for a webhook event
- **DeliveryAttempt**: Represents an individual retry attempt
- **SchemaDefinition**: Defines the schema for webhook payloads
- **HashedKey**: Stores API keys for authentication

**Aspire Integration Impact**: These entities continue to be stored in PostgreSQL. The only change is HOW the connection string is provided (via Aspire service discovery) rather than hardcoded in appsettings.json.

---

## Resource Topology Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                        Aspire AppHost                           │
│  (HookVerse.AppHost/Program.cs - Orchestration Topology)        │
└────────────────┬────────────────────────────────────────────────┘
                 │
         ┌───────┴───────┐
         │               │
    ┌────▼────┐    ┌─────▼─────┐
    │ Local   │    │ Production │
    │  Dev    │    │  (Azure)   │
    └────┬────┘    └─────┬──────┘
         │               │
         │               │
┌────────▼────────┐  ┌───▼──────────────────┐
│  Containers     │  │ Managed Services     │
│  (Docker)       │  │ (Azure)              │
├─────────────────┤  ├──────────────────────┤
│ • PostgreSQL    │  │ • Azure SQL Database │
│ • RabbitMQ      │  │ • Azure Service Bus  │
│ • Redis         │  │ • Azure Redis Cache  │
└─────────────────┘  └──────────────────────┘
         │                      │
         └──────────┬───────────┘
                    │
         ┌──────────▼──────────┐
         │   Service Projects   │
         │  (All reference     │
         │   ServiceDefaults)  │
         ├─────────────────────┤
         │ • HookVerse.Api     │
         │ • HookVerse.Worker  │
         │ • HookVerse.Dashboard│
         └─────────────────────┘
```

---

## Connection String Injection Flow

```
AppHost defines resource:
  builder.AddPostgres("postgres")
         │
         ▼
Aspire generates connection string:
  "Host=localhost;Port=5432;Database=hookverse;..."
         │
         ▼
Injected via configuration:
  ConnectionStrings:postgres = <generated-value>
         │
         ▼
Service retrieves connection:
  var connString = builder.Configuration.GetConnectionString("postgres");
         │
         ▼
EF Core uses connection:
  builder.Services.AddDbContext<AppDbContext>(options =>
      options.UseNpgsql(connString));
```

---

## Validation Rules Summary

| Entity | Rule | Enforcement |
|--------|------|-------------|
| AspireResourceDefinition | Name must be unique | Build-time (Aspire) |
| AspireResourceDefinition | CpuLimit format `\d+m` | Build-time (Aspire) |
| AspireResourceDefinition | MemoryLimit format `\d+(Mi\|Gi)` | Build-time (Aspire) |
| ServiceDependency | No circular dependencies | Build-time (Aspire) |
| ServiceDependency | References must exist | Build-time (Aspire) |
| ManifestOutputConfiguration | IncludeSecrets = false for prod | Policy/linting |
| ServiceDefaults | OTLP endpoint must be HTTPS | Runtime validation |

---

## Migration Impact

**Database Schema**: No changes. Existing tables (Webhooks, Deliveries, Attempts, etc.) remain unchanged.

**Configuration Changes**:
- **BEFORE (docker-compose.yml)**:
  ```yaml
  services:
    api:
      environment:
        - ConnectionStrings__Default=Host=postgres;Port=5432;...
  ```

- **AFTER (AppHost/Program.cs)**:
  ```csharp
  var postgres = builder.AddPostgres("postgres");
  builder.AddProject<HookVerse_Api>("api").WithReference(postgres);
  // Connection string automatically injected, no manual config needed
  ```

**Deployment Changes**:
- **BEFORE**: `docker-compose up` or `kubectl apply -f k8s/`
- **AFTER**: `dotnet run --project src/HookVerse.AppHost` (local) or `dotnet publish` → apply generated manifests (prod)

---

## Acceptance Criteria Mapping

| Data Model Element | Acceptance Criteria | Verification Method |
|--------------------|---------------------|---------------------|
| AspireResourceDefinition | AC-001: All 6 services defined | Unit test: Assert 6 services in AppHost |
| ServiceDependency | AC-002: Dependencies captured | Integration test: Verify service discovery works |
| ManifestOutputConfiguration | AC-003: K8s manifests generated | Test: Run `dotnet publish`, validate YAML |
| ManifestOutputConfiguration | AC-004: Bicep templates generated | Test: Run `dotnet publish`, validate Bicep |
| ServiceDefaults | AC-005: Health checks exposed | Integration test: HTTP GET `/health/ready` returns 200 |
| ServiceDefaults | AC-006: Telemetry exported | Integration test: Verify traces in Aspire dashboard |

---

## Summary

This feature introduces **configuration-based infrastructure entities** (not domain entities) for Aspire orchestration:
- **AspireResourceDefinition**: Services and resources in topology
- **ServiceDependency**: Dependency relationships
- **ManifestOutputConfiguration**: Deployment manifest settings
- **ServiceDefaults**: Shared service configuration

**No changes to existing webhook domain model**. Aspire integration is purely infrastructure-level, providing automatic service discovery, health checks, and observability without modifying business logic.
