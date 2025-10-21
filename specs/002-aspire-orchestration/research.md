# Research: .NET Aspire 9.5 Integration for HookVerse

**Date**: 2025-10-21  
**Feature**: 002-aspire-orchestration  
**Scope**: Research decisions for Aspire orchestration, manifest generation, and service integration

## Overview

This document captures research findings and decisions for integrating .NET Aspire 9.5 into the HookVerse webhook delivery platform. Focus areas: Aspire hosting patterns, container resource definitions, Azure/Kubernetes manifest generation, service defaults configuration, and testing strategies.

---

## R1: Aspire 9.5 Project Structure & Setup

### Decision
Create two new projects following Aspire conventions:
- **HookVerse.AppHost** (.NET Aspire App Host project)
- **HookVerse.ServiceDefaults** (Class library for shared service configuration)

### Rationale
This is the standard Aspire project structure recommended in official documentation. AppHost defines the orchestration topology, ServiceDefaults provides shared configuration (service discovery, health checks, OpenTelemetry) that all services reference. This pattern:
- Centralizes service topology in one place (AppHost/Program.cs)
- Avoids duplication of configuration across 6 service projects
- Enables Aspire's service discovery and observability features automatically
- Follows "convention over configuration" principle

### Alternatives Considered
- **Single AppHost only, no ServiceDefaults**: Would require duplicating service discovery/OpenTelemetry setup in each service's Program.cs. Rejected due to high maintenance burden (6 services × N lines of config).
- **Custom orchestration with Docker SDK**: Would require significant custom code for service discovery, health checks, manifest generation. Rejected because Aspire provides these capabilities out-of-the-box.

### Implementation Notes
- AppHost project references all 6 existing service projects
- ServiceDefaults project is referenced by all 6 service projects
- AppHost uses `builder.AddProject<T>()` to register services
- Services call `builder.AddServiceDefaults()` in their Program.cs

### References
- [.NET Aspire documentation: App host overview](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/app-host-overview)
- [Service defaults pattern](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/service-defaults)

---

## R2: Container Resource Definitions (Local Development)

### Decision
Define containerized dependencies in AppHost using Aspire hosting packages:
- **PostgreSQL**: `builder.AddPostgres("postgres").WithPgAdmin()` using `Aspire.Hosting.PostgreSQL` package
- **RabbitMQ**: `builder.AddRabbitMQ("rabbitmq").WithManagementPlugin()` using `Aspire.Hosting.RabbitMQ` package
- **Redis**: `builder.AddRedis("redis")` using `Aspire.Hosting.Redis` package

Connection strings automatically injected into services via environment variables.

### Rationale
Aspire hosting packages provide:
- Automatic container lifecycle management (pull, start, stop, health checks)
- Connection string generation and injection via configuration system
- Management UI plugins (PgAdmin, RabbitMQ Management) for debugging
- Version pinning through package versions
- Persistent volumes for data retention across restarts

This eliminates the need for docker-compose.yml and manual connection string configuration.

### Alternatives Considered
- **Docker Compose with Aspire**: Could keep docker-compose.yml and reference external containers. Rejected because it defeats the purpose of Aspire's integrated orchestration and service discovery.
- **Testcontainers for local dev**: Could use Testcontainers.NET for all environments. Rejected because Testcontainers is optimized for tests, not long-running local dev (no management UIs, less efficient resource usage).

### Implementation Notes
- Add NuGet packages to AppHost:
  - `Aspire.Hosting.PostgreSQL` (includes container + PgAdmin)
  - `Aspire.Hosting.RabbitMQ` (includes container + management plugin)
  - `Aspire.Hosting.Redis` (includes container)
- Connection string format: `ConnectionStrings:<resource-name>` in configuration
- Example: PostgreSQL connection injected as `ConnectionStrings:postgres`
- Services use `builder.Configuration.GetConnectionString("postgres")` to retrieve

### References
- [Aspire PostgreSQL component](https://learn.microsoft.com/en-us/dotnet/aspire/database/postgresql-component)
- [Aspire RabbitMQ component](https://learn.microsoft.com/en-us/dotnet/aspire/messaging/rabbitmq-component)
- [Aspire Redis component](https://learn.microsoft.com/en-us/dotnet/aspire/caching/redis-component)

---

## R3: Azure Managed Services Configuration (Production)

### Decision
Use Aspire's Azure hosting integrations for production resources:
- **Azure SQL Database**: `builder.AddAzureSqlServer("sql").AddDatabase("hookverse-db")`
- **Azure Service Bus**: `builder.AddAzureServiceBus("servicebus")`
- **Azure Cache for Redis**: `builder.AddAzureRedis("redis")`

Generated Bicep templates will provision these managed services. Connection strings reference Azure Key Vault secrets.

### Rationale
Per clarification #1, production should use Azure managed services for:
- Higher availability SLAs (99.99% vs self-managed containers)
- Automatic backups and point-in-time restore
- Built-in scaling and performance tuning
- Reduced operational overhead (no patching, monitoring, etc.)

Aspire's Azure hosting packages generate Bicep templates that provision these services with secure defaults (private endpoints, firewall rules, Key Vault integration).

### Alternatives Considered
- **Containerized databases in Azure Kubernetes Service**: Would provide environment parity but require significant operational overhead (backups, scaling, security hardening). Rejected per user clarification #1 (use managed services in production).
- **Separate Bicep templates**: Could manually write Bicep templates for Azure resources. Rejected because Aspire generates these from the same AppHost topology, ensuring dev/prod parity and reducing drift.

### Implementation Notes
- Add NuGet packages to AppHost:
  - `Aspire.Hosting.Azure.Sql`
  - `Aspire.Hosting.Azure.ServiceBus`
  - `Aspire.Hosting.Azure.Redis`
- Use `.PublishAsAzureContainerApp()` extension to target Azure Container Apps
- Connection strings stored in Azure Key Vault, referenced via Key Vault URIs
- Environment-specific parameters file (parameters.json) for resource names, SKUs

### References
- [Aspire Azure SQL component](https://learn.microsoft.com/en-us/dotnet/aspire/database/azure-sql-component)
- [Aspire Azure Service Bus](https://learn.microsoft.com/en-us/dotnet/aspire/messaging/azure-service-bus-component)
- [Azure deployment with Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/deployment/azure/overview)

---

## R4: Secret Management Strategy

### Decision
**Local Development**: Use .NET User Secrets and environment variables
- Secrets stored in `~/.microsoft/usersecrets/<project-id>/secrets.json`
- Aspire AppHost loads secrets via `builder.Configuration.AddUserSecrets<Program>()`
- No secrets in appsettings.json or version control

**Production (Azure)**: Use Azure Key Vault references in generated Bicep
- Aspire generates Key Vault resource in Bicep template
- Connection strings stored as Key Vault secrets
- Container Apps reference secrets using Key Vault URI syntax: `@Microsoft.KeyVault(VaultName=...;SecretName=...)`

**Production (Kubernetes)**: Use Kubernetes Secrets (not implemented initially per clarification #4 - deferred)

### Rationale
Per clarification #3, this hybrid approach provides:
- **Local development**: Simple, no Azure dependency for developers
- **Production**: Secure, centralized secret management with access control and audit logging
- **Separation of concerns**: Developers don't need production secret access

Aspire supports this pattern natively through environment-based configuration and manifest generation.

### Alternatives Considered
- **Azure Key Vault everywhere**: Would require all developers to have Azure access and Key Vault permissions. Rejected because it creates friction for local development and external contributors.
- **Secrets in config files with placeholders**: Would require manual token replacement in CI/CD. Rejected because it's error-prone and doesn't leverage Aspire's built-in secret management.

### Implementation Notes
- AppHost Program.cs: `builder.Configuration.AddUserSecrets<Program>()` for local
- Bicep template generation: Aspire automatically creates Key Vault and injects references
- No code changes needed in services (they use standard `IConfiguration`)
- Secret rotation: Update Key Vault secret → Container App automatically picks up new value (Azure handles refresh)

### References
- [User Secrets in .NET](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [Azure Key Vault with Container Apps](https://learn.microsoft.com/en-us/azure/container-apps/manage-secrets)

---

## R5: Manifest Generation for Kubernetes

### Decision
Use Aspire's `dotnet publish` with manifest format to generate Kubernetes YAML:
```bash
dotnet publish src/HookVerse.AppHost/HookVerse.AppHost.csproj \
  --output aspire/manifests/kubernetes \
  /p:PublishProfile=kubernetes
```

Generated manifests include:
- Deployments for each service (Api, Worker, Dashboard)
- Services for network exposure
- ConfigMaps for non-sensitive configuration
- Resource limits per clarification #5 (moderate defaults: 500m CPU, 512Mi memory)

### Rationale
Aspire 9.5 includes Kubernetes manifest generation as a built-in capability. This provides:
- Deterministic output (same AppHost → same YAML)
- Version control for manifests (commit to Git)
- Single source of truth (AppHost topology)
- Automatic service discovery configuration via environment variables

Per clarification #6, ingress/gateway configuration is deferred, so manifests will only include basic Deployment + Service resources.

### Alternatives Considered
- **Helm charts**: More flexible but adds complexity (templates, values, chart versioning). Rejected because Aspire's generated YAML is sufficient for initial deployment needs.
- **Kustomize overlays**: Good for multi-environment customization but requires additional tooling. Could be added later on top of Aspire-generated base manifests.
- **Manual YAML authoring**: Error-prone and doesn't benefit from Aspire's service discovery. Rejected.

### Implementation Notes
- Create publish profile: `src/HookVerse.AppHost/Properties/PublishProfiles/kubernetes.pubxml`
- Resource limits configurable via AppHost:
  ```csharp
  builder.AddProject<HookVerse_Api>("api")
    .WithReplicas(3)
    .WithCpuLimit("500m")
    .WithMemoryLimit("512Mi");
  ```
- Generated manifests checked into Git at `aspire/manifests/kubernetes/`
- CI/CD pipeline runs manifest generation on every build to detect drift

### References
- [Aspire Kubernetes deployment](https://learn.microsoft.com/en-us/dotnet/aspire/deployment/kubernetes)
- [Kubernetes resource limits](https://kubernetes.io/docs/concepts/configuration/manage-resources-containers/)

---

## R6: Manifest Generation for Azure (Bicep)

### Decision
Use Aspire's Azure publishing to generate Bicep templates:
```bash
dotnet publish src/HookVerse.AppHost/HookVerse.AppHost.csproj \
  --output aspire/manifests/azure \
  /p:PublishProfile=azure
```

Generated Bicep includes:
- Azure Container Apps for services (Api, Worker, Dashboard)
- Azure SQL Database, Service Bus, Redis Cache (managed services)
- Azure Key Vault for secrets
- Virtual Network with private endpoints (if configured)
- Application Insights for observability (integrates with OpenTelemetry)

### Rationale
Per clarifications #1 and #4, production Azure deployments should use:
- Managed services for data/messaging/caching (higher reliability, less ops overhead)
- Container Apps for application workloads (serverless scaling, integrated with Azure ecosystem)
- Infrastructure-as-code for repeatability and version control

Aspire generates Bicep that follows Azure best practices (private endpoints, managed identities, Key Vault integration).

### Alternatives Considered
- **ARM templates**: More verbose JSON format. Rejected because Bicep is the modern, recommended IaC language for Azure with better readability.
- **Terraform**: Popular multi-cloud tool. Rejected because Aspire natively generates Bicep, and HookVerse currently targets Azure/Kubernetes (not AWS/GCP).
- **Azure Portal manual provisioning**: Not repeatable or version-controllable. Rejected.

### Implementation Notes
- Create publish profile: `src/HookVerse.AppHost/Properties/PublishProfiles/azure.pubxml`
- Parameters file: `aspire/manifests/azure/parameters.json` for environment-specific values (resource names, SKUs, locations)
- Deployment command:
  ```bash
  az deployment group create \
    --resource-group hookverse-prod \
    --template-file aspire/manifests/azure/main.bicep \
    --parameters @aspire/manifests/azure/parameters.json
  ```
- CI/CD integration per clarification #7: Replace existing Azure deployment steps with manifest generation + `az deployment group create`

### References
- [Aspire Azure deployment guide](https://learn.microsoft.com/en-us/dotnet/aspire/deployment/azure/overview)
- [Azure Bicep documentation](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/overview)

---

## R7: Service Discovery & Configuration

### Decision
Use Aspire's built-in service discovery via `ServiceDefaults` extension methods:
- Services reference each other by logical name (not hardcoded URLs)
- Aspire injects service URIs via configuration: `services:<service-name>`
- HttpClient configuration uses named clients with service discovery:
  ```csharp
  builder.Services.AddHttpClient<IWorkerClient, WorkerClient>(
    client => client.BaseAddress = new Uri("http://worker"));
  ```

### Rationale
Hardcoded service URLs break in different environments (local, staging, prod). Aspire's service discovery:
- Works consistently across Docker (AppHost), Kubernetes (DNS), and Azure (Container Apps built-in service discovery)
- Eliminates manual connection string configuration (addresses clarification requirement: "zero manual connection string configuration")
- Enables local testing without editing appsettings.json

This directly supports User Story 2 (automatic service discovery) and Success Criteria SC-002 (zero manual connection strings).

### Alternatives Considered
- **Consul/Eureka**: External service discovery tools. Rejected because they add operational complexity (another service to run/manage) and Aspire provides this built-in.
- **Environment variables**: Could manually set `API_URL`, `WORKER_URL` env vars. Rejected because it's manual and error-prone (same problem we're solving).

### Implementation Notes
- ServiceDefaults project adds service discovery:
  ```csharp
  public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
  {
      builder.Services.AddServiceDiscovery();
      builder.Services.ConfigureHttpClientDefaults(http =>
      {
          http.AddServiceDiscovery();
      });
      return builder;
  }
  ```
- Services call `builder.AddServiceDefaults()` early in Program.cs
- Reference services by name in HttpClient configuration (Aspire resolves at runtime)

### References
- [Aspire service discovery](https://learn.microsoft.com/en-us/dotnet/aspire/service-discovery/overview)

---

## R8: OpenTelemetry Configuration

### Decision
Configure OpenTelemetry in `ServiceDefaults` with exporters controlled by environment:
- **Local development**: Export to Aspire dashboard (automatic, no configuration needed)
- **Production**: Export to user-chosen observability backend (Datadog, Application Insights, etc.) via OTLP

Per clarification #4, support pluggable exporters:
```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation())
    .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation());

// Exporter configured via environment variable
if (builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] != null)
{
    builder.Services.AddOpenTelemetry().UseOtlpExporter();
}
```

### Rationale
Per clarification #4: "OpenTelemetry with Aspire dashboard locally; user-configurable exporters (Datadog, Application Insights, etc.) in production."

This approach:
- Provides unified observability in local development (Aspire dashboard shows logs, traces, metrics)
- Gives users flexibility to choose their preferred production backend (avoids vendor lock-in)
- Uses OTLP (OpenTelemetry Protocol) standard for wide compatibility

Supports Constitution Principle VI (Observability & Monitoring) and User Story 3 (integrated observability dashboard).

### Alternatives Considered
- **Hardcode Application Insights**: Would force all users to use Azure. Rejected per clarification #4 (user choice).
- **Multiple exporters simultaneously**: Could export to Datadog + App Insights + Prometheus. Rejected due to complexity and cost (multiple ingestion bills).
- **No production observability**: Would violate Constitution Principle VI. Rejected.

### Implementation Notes
- ServiceDefaults configures tracing + metrics instrumentation
- Aspire dashboard automatically receives telemetry in local dev (no config needed)
- Production: Set `OTEL_EXPORTER_OTLP_ENDPOINT` environment variable to observability backend (Datadog: `https://otlp.datadoghq.com`, App Insights: `https://<region>.otlp.applicationinsights.azure.com`)
- Add NuGet package: `OpenTelemetry.Exporter.OpenTelemetryProtocol`

### References
- [Aspire dashboard telemetry](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)
- [Datadog OTLP ingestion](https://docs.datadoghq.com/opentelemetry/)

---

## R9: Health Checks Configuration

### Decision
Add health checks to all services in `ServiceDefaults`:
```csharp
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "ready" });

// For services with dependencies:
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("postgres"))
    .AddRabbitMQ(builder.Configuration.GetConnectionString("rabbitmq"))
    .AddRedis(builder.Configuration.GetConnectionString("redis"));
```

Expose health check endpoints:
- `/health/live` - Liveness probe (service is running)
- `/health/ready` - Readiness probe (service is ready to handle traffic)

### Rationale
Cloud-native principle (Constitution III) requires health checks for:
- Kubernetes liveness/readiness probes (auto-restart unhealthy pods)
- Azure Container Apps health probes (route traffic only to healthy instances)
- Aspire dashboard status visualization

Separating liveness vs readiness allows Kubernetes/Container Apps to distinguish between "restart me" (liveness failed) vs "don't send traffic yet" (readiness failed, e.g., DB connection not ready).

### Alternatives Considered
- **Single `/health` endpoint**: Simpler but doesn't allow separate liveness/readiness logic. Rejected because Kubernetes best practices recommend separate probes.
- **No health checks**: Would violate Constitution III and cause reliability issues (traffic routed to unhealthy instances). Rejected.

### Implementation Notes
- Add NuGet packages to ServiceDefaults:
  - `AspNetCore.HealthChecks.Npgsql`
  - `AspNetCore.HealthChecks.RabbitMQ`
  - `AspNetCore.HealthChecks.Redis`
- Map health check endpoints in Program.cs: `app.MapHealthChecks("/health/live", ...)` and `app.MapHealthChecks("/health/ready", ...)`
- Generated Kubernetes manifests include liveness/readiness probes automatically
- Aspire dashboard shows health status for all services

### References
- [ASP.NET Core health checks](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)
- [Kubernetes liveness/readiness probes](https://kubernetes.io/docs/tasks/configure-pod-container/configure-liveness-readiness-startup-probes/)

---

## R10: Testing Strategy for Aspire Infrastructure

### Decision
Create `HookVerse.AppHost.Tests` project with three test categories:

**1. Aspire Host Startup Tests** (validate AppHost starts successfully):
```csharp
[Fact]
public async Task AppHost_Starts_Successfully()
{
    var appHost = await DistributedApplicationTestingBuilder
        .CreateAsync<Projects.HookVerse_AppHost>();
    await using var app = await appHost.BuildAsync();
    await app.StartAsync();
    // Assert: No exceptions thrown
}
```

**2. Service Discovery Tests** (verify services can reach each other):
```csharp
[Fact]
public async Task Api_Can_Discover_Worker_Service()
{
    // Start AppHost, get API service URL, call worker via API
    // Assert: API successfully called Worker using service discovery
}
```

**3. Manifest Generation Tests** (validate generated YAML/Bicep):
```csharp
[Fact]
public void Generated_Kubernetes_Manifests_Are_Valid()
{
    // Run `dotnet publish` to generate manifests
    // Parse YAML and validate against Kubernetes schema
    // Assert: All required resources present (Deployment, Service)
}
```

### Rationale
Per Constitution Principle V (Test-First Development), infrastructure changes need acceptance tests before implementation. This addresses the ⚠️ PARTIAL status in Constitution Check.

Testable outcomes:
- ✅ Aspire host starts without errors (integration test)
- ✅ Services can discover and call each other (integration test)
- ✅ Generated manifests are valid and complete (schema validation test)
- ✅ Manifest generation is deterministic (snapshot testing)

These tests will be written FIRST (red phase), then implementation follows (green phase).

### Alternatives Considered
- **Manual testing only**: Would violate Constitution V (Test-First Development). Rejected.
- **End-to-end tests in real Kubernetes cluster**: Too slow for CI/CD (minutes vs seconds). Could be added later for pre-release validation but not part of TDD loop.
- **Unit tests for AppHost configuration**: AppHost is declarative configuration, not logic. Integration tests are more appropriate.

### Implementation Notes
- Add NuGet package: `Aspire.Hosting.Testing` (provides `DistributedApplicationTestingBuilder`)
- Tests run in CI/CD pipeline before deployment
- Manifest validation uses `kubectl --dry-run=client` for Kubernetes, `az bicep build` for Azure
- Snapshot testing for deterministic manifest generation (compare generated YAML to committed baseline)

### References
- [Testing Aspire apps](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/testing)
- [Kubernetes schema validation](https://github.com/instrumenta/kubernetes-json-schema)

---

## Summary of Research Outcomes

| Research ID | Topic | Status | Impact |
|-------------|-------|--------|--------|
| R1 | Aspire project structure | ✅ Resolved | 2 new projects: AppHost + ServiceDefaults |
| R2 | Container resources (local) | ✅ Resolved | PostgreSQL, RabbitMQ, Redis via Aspire hosting packages |
| R3 | Azure managed services (prod) | ✅ Resolved | Azure SQL, Service Bus, Redis Cache via Bicep |
| R4 | Secret management | ✅ Resolved | User Secrets (local), Key Vault (Azure) |
| R5 | Kubernetes manifest generation | ✅ Resolved | `dotnet publish` with kubernetes profile |
| R6 | Azure Bicep generation | ✅ Resolved | `dotnet publish` with azure profile |
| R7 | Service discovery | ✅ Resolved | Aspire built-in, reference by name |
| R8 | OpenTelemetry configuration | ✅ Resolved | Aspire dashboard (local), OTLP exporter (prod) |
| R9 | Health checks | ✅ Resolved | Liveness + readiness endpoints via ServiceDefaults |
| R10 | Testing strategy | ✅ Resolved | 3 test categories: startup, discovery, manifest validation |

**Conclusion**: All technical unknowns from Technical Context have been researched and resolved. No NEEDS CLARIFICATION items remain. Ready to proceed to Phase 1 (Design & Contracts).
