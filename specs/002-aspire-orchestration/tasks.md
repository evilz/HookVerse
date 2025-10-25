# Implementation Tasks: Simplified Development & Deployment with .NET Aspire

**Feature**: 002-aspire-orchestration  
**Branch**: `002-aspire-orchestration`  
**Date**: 2025-10-21

## Overview

This document breaks down the implementation of .NET Aspire 9.5 integration into granular, executable tasks organized by user story. Each task includes specific file paths and clear acceptance criteria.

**Tech Stack**: C# 13 / .NET 10, Aspire 9.5, ASP.NET Core 10, EF Core 10, OpenTelemetry  
**Dependencies**: PostgreSQL 15 (local), RabbitMQ 3.x (local), Redis 7.x (local), Azure managed services (production)  
**Testing**: xUnit, Testcontainers.NET, Aspire.Hosting.Testing

---

## User Story Mapping

| ID | User Story | Priority | Phase |
|----|------------|----------|-------|
| US1 | Quick Local Development Setup | P1 | 3 |
| US2 | Automatic Service Discovery | P2 | 4 |
| US3 | Integrated Observability Dashboard | P2 | 5 |
| US4 | Deployment Manifest Generation | P1 | 6 |
| US5 | Environment Parity & Configuration | P3 | 7 |

**MVP Scope**: US1 + US4 (local development + manifest generation for deployment readiness)

---

## Dependencies & Execution Order

```
Phase 1 (Setup)
    ↓
Phase 2 (Foundational - ServiceDefaults)
    ↓
Phase 3 (US1 - Local Dev Setup) ←─ BLOCKING: Required for all other stories
    ↓
    ├─→ Phase 4 (US2 - Service Discovery) ─┐
    ├─→ Phase 5 (US3 - Observability)      ├─→ Can run in parallel
    └─→ Phase 6 (US4 - Manifest Gen)       ┘
         ↓
    Phase 7 (US5 - Environment Parity)
         ↓
    Phase 8 (Polish & Migration)
```

**Parallelization Opportunities**:
- After US1: US2, US3, US4 can be developed in parallel (different concerns, minimal file overlap)
- Within each story: Tasks marked [P] can run in parallel

---

## Phase 1: Setup & Project Initialization

**Goal**: Create Aspire host and service defaults projects with proper structure.

**Prerequisites**: .NET 10 SDK with Aspire workload installed (`dotnet workload install aspire`)

### Tasks

- [X] T001 Create HookVerse.AppHost project using Aspire template in src/HookVerse.AppHost/
- [X] T002 Create HookVerse.ServiceDefaults class library in src/HookVerse.ServiceDefaults/
- [X] T003 Add HookVerse.AppHost to HookVerse.sln
- [X] T004 Add HookVerse.ServiceDefaults to HookVerse.sln
- [X] T005 Add NuGet package Aspire.Hosting to HookVerse.AppHost
- [X] T006 Add NuGet package Aspire.Hosting.PostgreSQL to HookVerse.AppHost
- [X] T007 Add NuGet package Aspire.Hosting.RabbitMQ to HookVerse.AppHost
- [X] T008 Add NuGet package Aspire.Hosting.Redis to HookVerse.AppHost
- [X] T009 Add NuGet package Microsoft.Extensions.ServiceDiscovery to HookVerse.ServiceDefaults
- [X] T010 Add NuGet package OpenTelemetry.Extensions.Hosting to HookVerse.ServiceDefaults
- [X] T011 Add NuGet package OpenTelemetry.Instrumentation.AspNetCore to HookVerse.ServiceDefaults
- [X] T012 Add NuGet package OpenTelemetry.Instrumentation.Http to HookVerse.ServiceDefaults
- [X] T013 Add NuGet package OpenTelemetry.Exporter.OpenTelemetryProtocol to HookVerse.ServiceDefaults
- [X] T014 Add NuGet package AspNetCore.HealthChecks.Npgsql to HookVerse.ServiceDefaults
- [X] T015 Add NuGet package AspNetCore.HealthChecks.RabbitMQ to HookVerse.ServiceDefaults
- [X] T016 Add NuGet package AspNetCore.HealthChecks.Redis to HookVerse.ServiceDefaults
- [X] T017 Create aspire/manifests/kubernetes/ directory structure
- [X] T018 Create aspire/manifests/azure/ directory structure
- [X] T019 Create aspire/templates/ directory for manifest generation scripts
- [X] T020 Create tests/HookVerse.AppHost.Tests/ project with xUnit
- [X] T021 Add NuGet package Aspire.Hosting.Testing to HookVerse.AppHost.Tests

**Completion Criteria**:
- ✅ Two new projects created and compile successfully
- ✅ All NuGet packages restored without errors
- ✅ Solution builds cleanly (`dotnet build`)
- ✅ Directory structure matches plan.md project structure

---

## Phase 2: Foundational - ServiceDefaults Implementation

**Goal**: Implement shared service configuration that all services will reference.

**Prerequisites**: Phase 1 complete

**Independent Test Criteria**:
- ServiceDefaults extension methods can be called without errors
- Health check endpoints are registered
- OpenTelemetry is configured with correct instrumentation

### Tasks

- [X] T022 Implement AddServiceDefaults extension method in src/HookVerse.ServiceDefaults/Extensions.cs
- [X] T023 Configure service discovery in AddServiceDefaults using AddServiceDiscovery()
- [X] T024 Configure HttpClient defaults with service discovery and resilience handler
- [X] T025 Configure OpenTelemetry tracing instrumentation (AspNetCore, HttpClient, EF Core)
- [X] T026 Configure OpenTelemetry metrics instrumentation (AspNetCore, HttpClient, Runtime)
- [X] T027 Implement conditional OTLP exporter based on OTEL_EXPORTER_OTLP_ENDPOINT environment variable
- [X] T028 Implement AddDefaultHealthChecks extension method in src/HookVerse.ServiceDefaults/Extensions.cs
- [X] T029 Add PostgreSQL health check in AddDefaultHealthChecks
- [X] T030 Add RabbitMQ health check in AddDefaultHealthChecks
- [X] T031 Add Redis health check in AddDefaultHealthChecks
- [X] T032 Create appsettings.json in src/HookVerse.ServiceDefaults/ with default OpenTelemetry configuration

**Completion Criteria**:
- ✅ ServiceDefaults project compiles without errors
- ✅ All extension methods are public and usable
- ✅ Health checks have appropriate tags ("live" vs "ready")
- ✅ OTLP exporter only activates when endpoint is configured

---

## Phase 3: US1 - Quick Local Development Setup (P1)

**User Story**: As a developer, I want to start all HookVerse services and dependencies with a single command (`dotnet run --project src/HookVerse.AppHost`) so I can begin development in under 2 minutes without manual Docker Compose configuration.

**Prerequisites**: Phase 2 complete

**Independent Test Criteria**:
- ✅ `dotnet run --project src/HookVerse.AppHost` starts all services without errors
- ✅ All container resources (PostgreSQL, RabbitMQ, Redis) are running and healthy
- ✅ All .NET services (Api, Worker, Dashboard) start successfully
- ✅ Aspire dashboard accessible at http://localhost:15888
- ✅ Total startup time < 2 minutes (Success Criteria SC-001)
- ✅ Health check endpoints return 200 OK for all services

### Tasks

#### Container Resource Configuration

- [X] T033 [P] [US1] Define PostgreSQL container resource in src/HookVerse.AppHost/Program.cs using AddPostgres()
- [X] T034 [P] [US1] Add PgAdmin management UI to PostgreSQL resource using WithPgAdmin()
- [X] T035 [P] [US1] Define RabbitMQ container resource in src/HookVerse.AppHost/Program.cs using AddRabbitMQ()
- [X] T036 [P] [US1] Add RabbitMQ Management Plugin using WithManagementPlugin()
- [X] T037 [P] [US1] Define Redis container resource in src/HookVerse.AppHost/Program.cs using AddRedis()

#### Service Project Registration

- [X] T038 [US1] Add project reference to HookVerse.Api in src/HookVerse.AppHost/HookVerse.AppHost.csproj
- [X] T039 [US1] Add project reference to HookVerse.Worker in src/HookVerse.AppHost/HookVerse.AppHost.csproj
- [X] T040 [US1] Add project reference to HookVerse.Dashboard in src/HookVerse.AppHost/HookVerse.AppHost.csproj
- [X] T041 [US1] Register HookVerse.Api project in AppHost using AddProject<Projects.HookVerse_Api>("api")
- [X] T042 [US1] Register HookVerse.Worker project in AppHost using AddProject<Projects.HookVerse_Worker>("worker")
- [X] T043 [US1] Register HookVerse.Dashboard project in AppHost using AddProject<Projects.HookVerse_Dashboard>("dashboard")

#### Service Dependencies

- [X] T044 [US1] Add PostgreSQL reference to Api project using WithReference(postgres)
- [X] T045 [US1] Add RabbitMQ reference to Api project using WithReference(rabbitmq)
- [X] T046 [US1] Add Redis reference to Api project using WithReference(redis)
- [X] T047 [US1] Add PostgreSQL reference to Worker project using WithReference(postgres)
- [X] T048 [US1] Add RabbitMQ reference to Worker project using WithReference(rabbitmq)
- [X] T049 [US1] Add Redis reference to Worker project using WithReference(redis)
- [X] T050 [US1] Add PostgreSQL reference to Dashboard project using WithReference(postgres)

#### Resource Limits Configuration

- [X] T051 [US1] Configure Api resource limits: 3 replicas, 500m CPU, 512Mi memory
- [X] T052 [US1] Configure Worker resource limits: 2 replicas, 250m CPU, 256Mi memory
- [X] T053 [US1] Configure Dashboard resource limits: 1 replica, 250m CPU, 256Mi memory

#### Service Integration

- [X] T054 [US1] Add ServiceDefaults reference to HookVerse.Api/HookVerse.Api.csproj
- [X] T055 [US1] Add ServiceDefaults reference to HookVerse.Worker/HookVerse.Worker.csproj
- [X] T056 [US1] Add ServiceDefaults reference to HookVerse.Dashboard/HookVerse.Dashboard.csproj
- [X] T057 [US1] Call builder.AddServiceDefaults() in src/HookVerse.Api/Program.cs (before other services)
- [X] T058 [US1] Call builder.AddServiceDefaults() in src/HookVerse.Worker/Program.cs (before other services)
- [X] T059 [US1] Call builder.AddServiceDefaults() in src/HookVerse.Dashboard/Program.cs (before other services)

#### Health Check Endpoints

- [X] T060 [US1] Add health checks to Api in src/HookVerse.Api/Program.cs using AddDefaultHealthChecks()
- [X] T061 [US1] Map /health/live endpoint in src/HookVerse.Api/Program.cs with "live" tag filter
- [X] T062 [US1] Map /health/ready endpoint in src/HookVerse.Api/Program.cs with "ready" tag filter
- [X] T063 [P] [US1] Add health checks to Worker in src/HookVerse.Worker/Program.cs using AddDefaultHealthChecks()
- [X] T064 [P] [US1] Map /health/live endpoint in src/HookVerse.Worker/Program.cs with "live" tag filter
- [X] T065 [P] [US1] Map /health/ready endpoint in src/HookVerse.Worker/Program.cs with "ready" tag filter
- [X] T066 [P] [US1] Add health checks to Dashboard in src/HookVerse.Dashboard/Program.cs using AddDefaultHealthChecks()
- [X] T067 [P] [US1] Map /health/live endpoint in src/HookVerse.Dashboard/Program.cs with "live" tag filter
- [X] T068 [P] [US1] Map /health/ready endpoint in src/HookVerse.Dashboard/Program.cs with "ready" tag filter

#### Launch Configuration

- [X] T069 [US1] Create launchSettings.json in src/HookVerse.AppHost/Properties/ with Aspire dashboard profile
- [X] T070 [US1] Configure Aspire dashboard URL to http://localhost:15888 in launchSettings.json

#### Integration Tests

- [X] T071 [US1] Create AspireHostStartupTests.cs in tests/HookVerse.AppHost.Tests/
- [X] T072 [US1] Implement test: AppHost_Starts_Successfully using DistributedApplicationTestingBuilder
- [X] T073 [US1] Implement test: All_Services_Are_Registered to verify 6 resources (3 containers + 3 projects)
- [X] T074 [US1] Implement test: PostgreSQL_Container_Starts to verify postgres resource is running
- [X] T075 [US1] Implement test: RabbitMQ_Container_Starts to verify rabbitmq resource is running
- [X] T076 [US1] Implement test: Redis_Container_Starts to verify redis resource is running

**Completion Criteria**:
- ✅ All T033-T076 tasks completed
- ✅ `dotnet run --project src/HookVerse.AppHost` starts successfully
- ✅ Aspire dashboard shows all 6 resources in "Running" state
- ✅ Health check endpoints return 200 OK: curl https://localhost:7001/health/ready
- ✅ Integration tests pass: `dotnet test tests/HookVerse.AppHost.Tests/`
- ✅ Startup time measured and < 2 minutes

---

## Phase 4: US2 - Automatic Service Discovery (P2)

**User Story**: As a developer, I want services to discover each other automatically without hardcoded URLs so I can test service-to-service communication locally and in deployed environments without configuration changes.

**Prerequisites**: Phase 3 (US1) complete

**Independent Test Criteria**:
- ✅ Api can call Worker using service name "http://worker" without hardcoded URL
- ✅ Worker can call Api using service name "http://api" without hardcoded URL
- ✅ Connection strings for PostgreSQL, RabbitMQ, Redis are auto-injected (zero manual config)
- ✅ Service discovery works in local development (Aspire resolves names)
- ✅ No hardcoded connection strings or URLs in appsettings.json (Success Criteria SC-002)

### Tasks

#### Connection String Migration

- [X] T077 [P] [US2] Remove hardcoded PostgreSQL connection string from src/HookVerse.Api/appsettings.json
- [X] T078 [P] [US2] Remove hardcoded PostgreSQL connection string from src/HookVerse.Worker/appsettings.json
- [X] T079 [P] [US2] Remove hardcoded PostgreSQL connection string from src/HookVerse.Dashboard/appsettings.json
- [X] T080 [P] [US2] Remove hardcoded RabbitMQ connection string from src/HookVerse.Api/appsettings.json
- [X] T081 [P] [US2] Remove hardcoded RabbitMQ connection string from src/HookVerse.Worker/appsettings.json
- [X] T082 [P] [US2] Remove hardcoded Redis connection string from src/HookVerse.Api/appsettings.json
- [X] T083 [P] [US2] Remove hardcoded Redis connection string from src/HookVerse.Worker/appsettings.json

#### DbContext Configuration Update

- [X] T084 [US2] Update DbContext configuration in src/HookVerse.Infrastructure/Extensions/ServiceCollectionExtensions.cs to use GetConnectionString("postgres")
- [X] T085 [US2] Remove connection string fallbacks and defaults (rely on Aspire injection)

#### HttpClient Service Discovery Configuration

- [X] T086 [US2] Configure HttpClient for Worker calls in src/HookVerse.Api/Program.cs with BaseAddress = new Uri("http://worker")
- [X] T087 [US2] Configure HttpClient for Api calls in src/HookVerse.Worker/Program.cs with BaseAddress = new Uri("http://api")

#### Integration Tests

- [X] T088 [US2] Create ServiceDiscoveryTests.cs in tests/HookVerse.AppHost.Tests/
- [X] T089 [US2] Implement test: Api_Can_Discover_Worker_Service to verify Api → Worker communication
- [X] T090 [US2] Implement test: Worker_Can_Discover_Api_Service to verify Worker → Api communication
- [X] T091 [US2] Implement test: Connection_Strings_Are_Injected to verify all services have postgres, rabbitmq, redis connections
- [X] T092 [US2] Implement test: No_Hardcoded_Connection_Strings to scan appsettings.json files for hardcoded values

**Completion Criteria**:
- ✅ All T077-T092 tasks completed
- ✅ Zero hardcoded connection strings in appsettings.json files
- ✅ Services successfully communicate using service names
- ✅ Integration tests pass: `dotnet test tests/HookVerse.AppHost.Tests/ServiceDiscoveryTests.cs`
- ✅ Success Criteria SC-002 met: Zero manual connection string configuration

---

## Phase 5: US3 - Integrated Observability Dashboard (P2)

**User Story**: As a developer, I want a unified observability dashboard showing logs, traces, and metrics from all services so I can debug issues without switching between multiple tools.

**Prerequisites**: Phase 3 (US1) complete (independent of US2)

**Independent Test Criteria**:
- ✅ Aspire dashboard shows logs from all 3 services (Api, Worker, Dashboard)
- ✅ Distributed traces appear in Aspire dashboard with trace IDs
- ✅ Metrics visible in dashboard (request rates, latency percentiles)
- ✅ OTLP exporter can be configured for production (Datadog, Application Insights)
- ✅ Telemetry data is structured and filterable

### Tasks

#### OpenTelemetry Configuration

- [X] T093 [P] [US3] Verify OpenTelemetry tracing is enabled in ServiceDefaults (already done in T025)
- [X] T094 [P] [US3] Verify OpenTelemetry metrics are enabled in ServiceDefaults (already done in T026)
- [X] T095 [P] [US3] Add EF Core instrumentation to capture database query traces

#### Activity Source Registration

- [X] T096 [P] [US3] Register custom ActivitySource "HookVerse.Api" in src/HookVerse.Api/Program.cs
- [X] T097 [P] [US3] Register custom ActivitySource "HookVerse.Worker" in src/HookVerse.Worker/Program.cs
- [X] T098 [P] [US3] Register custom ActivitySource "HookVerse.Dashboard" in src/HookVerse.Dashboard/Program.cs

#### Structured Logging Configuration

- [ ] T099 [US3] Configure structured logging in ServiceDefaults with JSON formatter
- [ ] T100 [US3] Add correlation ID middleware to Api in src/HookVerse.Api/Middleware/ for trace correlation
- [ ] T101 [US3] Add correlation ID middleware to Worker in src/HookVerse.Worker/Middleware/ for trace correlation

#### OTLP Exporter Configuration

- [ ] T102 [US3] Create appsettings.Production.json in src/HookVerse.Api/ with OTEL_EXPORTER_OTLP_ENDPOINT placeholder
- [ ] T103 [US3] Create appsettings.Production.json in src/HookVerse.Worker/ with OTEL_EXPORTER_OTLP_ENDPOINT placeholder
- [ ] T104 [US3] Document OTLP exporter configuration in README.md for Datadog and Application Insights

#### Dashboard Verification

- [ ] T105 [US3] Verify Aspire dashboard displays logs from all services at http://localhost:15888
- [ ] T106 [US3] Verify Aspire dashboard displays distributed traces with trace IDs
- [ ] T107 [US3] Verify Aspire dashboard displays metrics (CPU, memory, request rates)

#### Integration Tests

- [ ] T108 [US3] Create ObservabilityTests.cs in tests/HookVerse.AppHost.Tests/
- [ ] T109 [US3] Implement test: Traces_Are_Exported to verify telemetry collection
- [ ] T110 [US3] Implement test: Metrics_Are_Collected to verify metric instrumentation
- [ ] T111 [US3] Implement test: OTLP_Exporter_Activates_When_Configured to verify conditional exporter

**Completion Criteria**:
- ✅ All T093-T111 tasks completed
- ✅ Aspire dashboard accessible and shows all telemetry
- ✅ Distributed traces include trace IDs and span relationships
- ✅ OTLP exporter documented and configurable
- ✅ Integration tests pass: `dotnet test tests/HookVerse.AppHost.Tests/ObservabilityTests.cs`

---

## Phase 6: US4 - Deployment Manifest Generation (P1)

**User Story**: As a DevOps engineer, I want to generate Kubernetes YAML and Azure Bicep templates from the Aspire configuration so I can deploy HookVerse to production without manually writing infrastructure-as-code.

**Prerequisites**: Phase 3 (US1) complete (independent of US2, US3)

**Independent Test Criteria**:
- ✅ `dotnet publish` generates valid Kubernetes manifests in aspire/manifests/kubernetes/
- ✅ Generated K8s manifests include Deployments, Services, ConfigMaps
- ✅ `dotnet publish` generates valid Azure Bicep templates in aspire/manifests/azure/
- ✅ Generated Bicep includes Container Apps, SQL Database, Service Bus, Redis Cache, Key Vault
- ✅ Manifest generation completes in < 30 seconds (Success Criteria SC-004)
- ✅ Generated manifests pass schema validation

### Tasks

#### Kubernetes Publish Profile

- [X] T112 [US4] Create PublishProfiles directory in src/HookVerse.AppHost/Properties/PublishProfiles/
- [X] T113 [US4] Create kubernetes.pubxml in src/HookVerse.AppHost/Properties/PublishProfiles/ for K8s target
- [X] T114 [US4] Configure output path to aspire/manifests/kubernetes/ in kubernetes.pubxml

#### Azure Publish Profile

- [X] T115 [P] [US4] Create azure.pubxml in src/HookVerse.AppHost/Properties/PublishProfiles/ for Azure target
- [X] T116 [P] [US4] Configure output path to aspire/manifests/azure/ in azure.pubxml
- [X] T117 [P] [US4] Add NuGet package Aspire.Hosting.Azure.Sql to HookVerse.AppHost
- [X] T118 [P] [US4] Add NuGet package Aspire.Hosting.Azure.ServiceBus to HookVerse.AppHost
- [X] T119 [P] [US4] Add NuGet package Aspire.Hosting.Azure.Redis to HookVerse.AppHost

#### Azure Resource Configuration

- [X] T120 [US4] Define Azure SQL Server resource in AppHost/Program.cs using AddAzureSqlServer("sql")
- [X] T121 [US4] Add hookverse database to Azure SQL Server using AddDatabase("hookverse-db")
- [X] T122 [US4] Define Azure Service Bus resource in AppHost/Program.cs using AddAzureServiceBus("servicebus")
- [X] T123 [US4] Define Azure Redis Cache resource in AppHost/Program.cs using AddAzureRedis("redis-cache")
- [X] T124 [US4] Configure services to reference Azure resources when PublishProfile=azure
- [X] T125 [US4] Add PublishAsAzureContainerApp() extension to Api project for Azure Container Apps deployment

#### Manifest Generation Scripts

- [X] T126 [US4] Create generate-k8s-manifests.ps1 in aspire/templates/ for Kubernetes manifest generation
- [X] T127 [US4] Create generate-azure-manifests.ps1 in aspire/templates/ for Azure Bicep generation
- [X] T128 [US4] Add manifest validation step using kubectl --dry-run=client in generate-k8s-manifests.ps1
- [X] T129 [US4] Add Bicep validation step using az bicep build in generate-azure-manifests.ps1

#### Parameters File

- [X] T130 [US4] Create parameters.json template in aspire/manifests/azure/ with environment-specific values
- [X] T131 [US4] Document parameter customization in aspire/manifests/azure/README.md

#### Schema Validation Tests

- [X] T132 [US4] Create ManifestGenerationTests.cs in tests/HookVerse.AppHost.Tests/
- [X] T133 [US4] Implement test: Kubernetes_Manifests_Are_Generated to verify K8s YAML creation
- [X] T134 [US4] Implement test: Kubernetes_Manifests_Are_Valid to validate YAML against K8s schema
- [X] T135 [US4] Implement test: Azure_Bicep_Is_Generated to verify Bicep template creation
- [X] T136 [US4] Implement test: Azure_Bicep_Is_Valid to validate Bicep syntax
- [X] T137 [US4] Implement test: Manifest_Generation_Performance to measure < 30 second constraint
- [X] T138 [US4] Implement test: Generated_Manifests_Are_Deterministic using snapshot testing

#### Resource Limits in Manifests

- [X] T139 [US4] Verify generated K8s manifests include CPU/memory limits (500m/512Mi for Api)
- [X] T140 [US4] Verify generated K8s manifests include health probes (liveness + readiness)
- [X] T141 [US4] Verify generated Bicep includes resource limits for Container Apps

**Completion Criteria**:
- ✅ All T112-T141 tasks completed
- ✅ Kubernetes manifests generated successfully via `dotnet publish`
- ✅ Azure Bicep templates generated successfully via `dotnet publish`
- ✅ All validation tests pass (schema, syntax, performance)
- ✅ Integration tests pass: `dotnet test tests/HookVerse.AppHost.Tests/ManifestGenerationTests.cs`
- ✅ Success Criteria SC-004 met: Manifest generation < 30 seconds

---

## Phase 7: US5 - Environment Parity & Configuration (P3)

**User Story**: As a platform engineer, I want consistent configuration management across local, staging, and production environments so deployments behave predictably regardless of where they run.

**Prerequisites**: Phase 3, 4, 6 complete (US1, US2, US4)

**Independent Test Criteria**:
- ✅ Connection string abstraction works across container and managed service backends
- ✅ Local development uses containers, production uses Azure managed services (no code changes)
- ✅ Secrets managed via User Secrets (local) and Key Vault (production)
- ✅ Environment-specific configuration is externalized, not hardcoded

### Tasks

#### Connection String Abstraction

- [ ] T142 [US5] Implement connection string provider interface in src/HookVerse.Infrastructure/Configuration/IConnectionStringProvider.cs
- [ ] T143 [US5] Implement LocalConnectionStringProvider for container-based connections
- [ ] T144 [US5] Implement AzureConnectionStringProvider for managed service connections
- [ ] T145 [US5] Register appropriate provider based on environment in Program.cs

#### Secret Management Configuration

- [ ] T146 [P] [US5] Add User Secrets support to AppHost in src/HookVerse.AppHost/Program.cs
- [ ] T147 [P] [US5] Create example secrets.json template in aspire/templates/user-secrets.template.json
- [ ] T148 [P] [US5] Configure Key Vault references in generated Bicep for production secrets

#### Environment-Specific Configuration

- [ ] T149 [US5] Create appsettings.Development.json with container connection defaults
- [ ] T150 [US5] Create appsettings.Staging.json with Azure managed service defaults
- [ ] T151 [US5] Create appsettings.Production.json with Azure managed service defaults
- [ ] T152 [US5] Document environment variable overrides in README.md

#### Configuration Validation

- [ ] T153 [US5] Implement configuration validation on startup to detect missing required settings
- [ ] T154 [US5] Add health check for Key Vault connectivity in production environments

#### Integration Tests

- [ ] T155 [US5] Create EnvironmentParityTests.cs in tests/HookVerse.AppHost.Tests/
- [ ] T156 [US5] Implement test: Local_Uses_Container_Connections to verify Development profile
- [ ] T157 [US5] Implement test: Production_Uses_Managed_Services to verify Azure profile
- [ ] T158 [US5] Implement test: Secrets_Not_In_Version_Control to scan for exposed secrets

**Completion Criteria**:
- ✅ All T142-T158 tasks completed
- ✅ Connection string abstraction switches between containers and managed services automatically
- ✅ User Secrets configured for local development
- ✅ Key Vault references in generated Bicep templates
- ✅ Integration tests pass: `dotnet test tests/HookVerse.AppHost.Tests/EnvironmentParityTests.cs`

---

## Phase 8: Polish & Migration

**Goal**: Replace legacy Docker Compose files, update CI/CD pipelines, and finalize documentation.

**Prerequisites**: All user story phases (3-7) complete

### Tasks

#### Legacy Cleanup

- [ ] T159 Remove docker-compose.yml from repository root (replaced by Aspire)
- [ ] T160 Remove start-services.ps1 from repository root (replaced by dotnet run --project AppHost)
- [ ] T161 Remove start-services.sh from repository root (replaced by dotnet run --project AppHost)
- [ ] T162 Remove docker/ directory if it contains custom Dockerfiles (use Aspire defaults)
- [ ] T163 Update .gitignore to include aspire/manifests/ generated files

#### CI/CD Pipeline Updates

- [ ] T164 Update .github/workflows/ci.yml to install Aspire workload
- [ ] T165 Add manifest generation step to CI pipeline using dotnet publish
- [ ] T166 Add manifest validation step to CI pipeline (kubectl --dry-run, az bicep build)
- [ ] T167 Update deployment workflow to use generated manifests
- [ ] T168 Replace existing kubectl apply steps with Aspire-generated manifests
- [ ] T169 Add Azure deployment step using generated Bicep templates

#### Documentation Updates

- [ ] T170 Update README.md with new getting started instructions (dotnet run --project AppHost)
- [ ] T171 Update DEVELOPMENT.md with Aspire development workflow
- [ ] T172 Update RUNNING.md with Aspire dashboard usage
- [ ] T173 Create DEPLOYMENT.md documenting manifest generation and deployment process
- [ ] T174 Update API-GUIDE.md with service discovery patterns
- [ ] T175 Document observability configuration in docs/observability.md
- [ ] T176 Create troubleshooting guide in docs/troubleshooting-aspire.md

#### Performance Validation

- [ ] T177 Measure and document startup time (verify < 2 minutes)
- [ ] T178 Measure and document hot-reload performance (verify < 5 seconds)
- [ ] T179 Measure and document manifest generation time (verify < 30 seconds)
- [ ] T180 Create performance baseline document in docs/performance-baseline.md

#### Final Verification

- [ ] T181 Run full integration test suite and verify all tests pass
- [ ] T182 Verify all success criteria from spec.md are met
- [ ] T183 Perform end-to-end smoke test: start locally → make code change → deploy to staging
- [ ] T184 Update .github/copilot-instructions.md with Aspire commands and patterns

**Completion Criteria**:
- ✅ All T159-T184 tasks completed
- ✅ Docker Compose files removed
- ✅ CI/CD pipelines updated and passing
- ✅ All documentation updated
- ✅ Performance targets validated
- ✅ End-to-end workflow tested successfully

---

## Task Summary

| Phase | Task Range | Count | User Story | Can Parallelize |
|-------|------------|-------|------------|-----------------|
| 1: Setup | T001-T021 | 21 | - | Some |
| 2: Foundational | T022-T032 | 11 | - | Some |
| 3: US1 | T033-T076 | 44 | Quick Local Dev Setup (P1) | Many |
| 4: US2 | T077-T092 | 16 | Service Discovery (P2) | Many |
| 5: US3 | T093-T111 | 19 | Observability (P2) | Many |
| 6: US4 | T112-T141 | 30 | Manifest Generation (P1) | Some |
| 7: US5 | T142-T158 | 17 | Environment Parity (P3) | Some |
| 8: Polish | T159-T184 | 26 | - | Many |
| **TOTAL** | T001-T184 | **184** | 5 Stories | ~40% parallel |

---

## Parallel Execution Examples

### Phase 3 (US1) - Example Parallelization

**Team of 3 developers**:
- Dev 1: Container resources (T033-T037) → Service registration (T038-T043) → Integration tests (T071-T076)
- Dev 2: Service dependencies (T044-T050) → Resource limits (T051-T053) → Launch config (T069-T070)
- Dev 3: Service integration (T054-T059) → Health checks (T060-T068)

**Merge point**: All three streams complete → Test US1 independently

### Cross-Story Parallelization (After US1)

**Team of 3 developers**:
- Dev 1: US2 Service Discovery (T077-T092) - 16 tasks
- Dev 2: US3 Observability (T093-T111) - 19 tasks
- Dev 3: US4 Manifest Generation (T112-T141) - 30 tasks

**Merge point**: All three stories complete → Test together → Proceed to US5

---

## Implementation Strategy

### MVP Scope (Recommended)

**Goal**: Deliver minimum viable Aspire integration for local development and deployment readiness.

**Include**:
- ✅ Phase 1: Setup (T001-T021)
- ✅ Phase 2: Foundational (T022-T032)
- ✅ Phase 3: US1 - Local Dev Setup (T033-T076)
- ✅ Phase 6: US4 - Manifest Generation (T112-T141)

**Defer to v2**:
- Phase 4: US2 - Service Discovery (can continue using existing connection strings temporarily)
- Phase 5: US3 - Observability (Aspire dashboard works by default, advanced config can wait)
- Phase 7: US5 - Environment Parity (nice-to-have, not blocking)

**MVP Deliverables**:
- `dotnet run --project src/HookVerse.AppHost` starts all services
- Kubernetes and Azure manifests can be generated
- Core Aspire functionality validated

**Time Estimate**: ~80 tasks (Setup + Foundational + US1 + US4) × 2-4 hours/task = **160-320 hours** (~4-8 weeks for 1 developer, ~2-4 weeks for 2 developers in parallel)

### Full Feature Delivery

**All Phases**: 184 tasks × 2-4 hours = **368-736 hours** (~9-18 weeks for 1 developer, ~4-8 weeks for 3 developers in parallel)

---

## Validation Checklist

Before marking feature complete, verify:

- [ ] All 184 tasks completed and checked off
- [ ] All integration tests passing (`dotnet test`)
- [ ] Success Criteria from spec.md validated:
  - [ ] SC-001: Startup time < 2 minutes
  - [ ] SC-002: Zero manual connection strings
  - [ ] SC-003: Hot reload < 5 seconds
  - [ ] SC-004: Manifest generation < 30 seconds
- [ ] Constitution principles satisfied:
  - [ ] Test-First Development: All tests written and passing
  - [ ] Cloud-Native: Health checks, resource limits, service discovery
  - [ ] Observability: Telemetry exported, dashboard functional
  - [ ] Security: Secrets in Key Vault, no plain text
- [ ] Documentation complete: README, DEVELOPMENT, DEPLOYMENT, API-GUIDE
- [ ] CI/CD pipelines updated and passing
- [ ] Docker Compose files removed
- [ ] End-to-end smoke test passed

---

## Notes for Implementers

1. **Test-First Development**: Per Constitution Principle V, write integration tests (T071-T076, T088-T092, etc.) BEFORE implementing the feature code.

2. **Incremental Commits**: Commit after each logical group of tasks (e.g., after completing container resources, after service registration).

3. **Branch Strategy**: Follow feature branch `002-aspire-orchestration`. Create sub-branches for parallel work if needed (e.g., `002-aspire-orchestration-us2`, `002-aspire-orchestration-us3`).

4. **File Paths**: All file paths are absolute and match the project structure from plan.md. Verify paths exist before implementing.

5. **Dependencies**: Respect task dependencies. Do NOT skip foundational tasks (Phase 2) before user story tasks (Phase 3+).

6. **Validation**: After each phase, run integration tests and verify independent test criteria before proceeding.

7. **Performance**: Measure performance targets (startup time, manifest generation) early and often to avoid surprises.

8. **Documentation**: Update docs as you implement, not at the end.

---

**Ready to start implementation!** Begin with Phase 1: Setup (T001-T021).
