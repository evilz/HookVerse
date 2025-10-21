# Implementation Plan: Simplified Development & Deployment with .NET Aspire

**Branch**: `002-aspire-orchestration` | **Date**: 2025-10-21 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/002-aspire-orchestration/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Replace complex Docker Compose and Kubernetes configurations with .NET Aspire 9.5 for streamlined local development and automated deployment manifest generation. Aspire will orchestrate containerized dependencies (PostgreSQL, RabbitMQ, Redis) for local development, enable service discovery with zero manual configuration, provide integrated observability dashboard, and generate deployment manifests for Azure (Bicep templates with managed services) and Kubernetes (YAML with configurable resources). Migration strategy: Big bang replacement of existing Docker Compose/K8s files once Aspire setup is validated.

## Technical Context

**Language/Version**: C# 13 / .NET 10  
**Primary Dependencies**: .NET Aspire 9.5 (orchestration), ASP.NET Core 10 (API), EF Core 10 (data access), OpenTelemetry (observability)  
**Storage**: PostgreSQL 15 (local containers), Azure SQL Database (production managed service)  
**Message Broker**: RabbitMQ 3.x (local containers), Azure Service Bus (production managed service)  
**Caching**: Redis 7.x (local containers), Azure Cache for Redis (production managed service)  
**Testing**: xUnit (unit/integration tests), Testcontainers.NET (containerized test dependencies)  
**Target Platform**: Linux containers (Docker/Podman for local), Kubernetes (on-premises/cloud), Azure Container Apps (production)
**Project Type**: Distributed microservices (Web API + Background Worker + Dashboard)  
**Performance Goals**: <2 minute full stack startup locally, <30 second manifest generation, <5 second hot-reload for code changes  
**Constraints**: Zero manual connection string configuration, zero Docker Compose editing for new dependencies, manifest generation must be deterministic and version-controllable  
**Scale/Scope**: 6 existing .NET projects (Api, Core, Infrastructure, Shared, Worker, Dashboard), 3 external dependencies (PostgreSQL, RabbitMQ, Redis), 2 deployment targets (Azure + Kubernetes)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### I. API-First Design
**Status**: ✅ PASS (N/A for infrastructure feature)  
**Justification**: This feature focuses on developer experience and deployment tooling, not user-facing APIs. Aspire itself exposes no APIs; it generates configurations. No API contracts required.

### II. Reliability & Fault Tolerance
**Status**: ✅ PASS  
**Justification**: Aspire orchestration enhances reliability by standardizing service discovery and health checks. Generated manifests will include health probes, restart policies, and resource limits. No negative impact on existing webhook delivery guarantees.

### III. Cloud-Native Architecture
**Status**: ✅ PASS (ENHANCED)  
**Justification**: This feature strengthens cloud-native principles by:
- Externalizing service configuration via Aspire resource definitions
- Standardizing health check endpoints across all services
- Enabling horizontal scaling through container orchestration
- Generating infrastructure-as-code manifests (Bicep, K8s YAML)

### IV. Open Source Transparency
**Status**: ✅ PASS  
**Justification**: All Aspire configurations, generated manifests, and orchestration code will be in public repository. No proprietary dependencies (.NET Aspire is open source). Decision rationale documented in this plan.

### V. Test-First Development
**Status**: ✅ PASS  
**Justification**: Infrastructure/tooling changes have different testing needs than application logic. Testable outcomes defined in research.md (R10):
- Aspire host starts successfully (integration test via `DistributedApplicationTestingBuilder`)
- Generated manifests are valid (schema validation tests via `kubectl --dry-run` and `az bicep build`)
- Services can discover each other (service discovery integration test)
- Manifest generation is deterministic (snapshot testing)
- Health check endpoints respond correctly (HTTP integration tests)
**Test Project**: `HookVerse.AppHost.Tests` will be created with acceptance tests BEFORE implementation (TDD red-green-refactor).

### VI. Observability & Monitoring
**Status**: ✅ PASS (ENHANCED)  
**Justification**: Aspire's built-in dashboard provides unified observability for local development. OpenTelemetry integration ensures production traces/metrics export to user-chosen backends (Datadog, Application Insights). This feature improves observability posture.

### VII. Security by Design
**Status**: ✅ PASS  
**Justification**: Aspire configuration references Azure Key Vault for production secrets (per clarification #3). Local development uses user-secrets/environment variables. No secrets will be embedded in manifests or version control. Generated manifests include resource limits to prevent resource exhaustion attacks.

**Overall Gate Status**: ✅ PASS - All constitution principles satisfied. Test strategy defined in research.md. Ready to proceed to implementation.

## Project Structure

### Documentation (this feature)

```
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```
src/
├── HookVerse.Api/                    # Main REST API (existing)
├── HookVerse.Core/                   # Domain logic (existing)
├── HookVerse.Dashboard/              # Admin dashboard (existing)
├── HookVerse.Infrastructure/         # Data access (existing)
├── HookVerse.Shared/                 # Cross-cutting concerns (existing)
├── HookVerse.Worker/                 # Background job processor (existing)
└── HookVerse.AppHost/                # NEW: Aspire orchestration host
    ├── Program.cs                    # Aspire app host entry point
    ├── Properties/
    │   └── launchSettings.json       # Launch profiles with Aspire dashboard
    └── appsettings.json              # Aspire-specific configuration

src/HookVerse.ServiceDefaults/        # NEW: Shared Aspire service defaults
├── Extensions.cs                     # Service discovery, health checks, OpenTelemetry
└── appsettings.json                  # Default observability configuration

aspire/                               # NEW: Generated manifests and templates
├── manifests/
│   ├── kubernetes/
│   │   ├── deployment.yaml           # K8s deployments for all services
│   │   ├── service.yaml              # K8s services
│   │   └── configmap.yaml            # Configuration
│   └── azure/
│       ├── main.bicep                # Azure Container Apps + managed services
│       └── parameters.json           # Environment-specific parameters
└── templates/
    └── manifest-generation.ps1       # Script to regenerate manifests

tests/
├── HookVerse.AppHost.Tests/          # NEW: Aspire orchestration tests
│   ├── AspireHostStartupTests.cs     # Validate host starts successfully
│   ├── ServiceDiscoveryTests.cs      # Verify service-to-service communication
│   └── ManifestGenerationTests.cs    # Validate generated manifests
└── [existing test projects]          # HookVerse.Api.Tests, etc.

# Files to be REMOVED (replaced by Aspire)
docker-compose.yml                    # DEPRECATED: Replaced by Aspire AppHost
start-services.ps1                    # DEPRECATED: Replaced by `dotnet run --project src/HookVerse.AppHost`
start-services.sh                     # DEPRECATED: Same as above
docker/                               # DEPRECATED: Custom Dockerfiles if any (use Aspire defaults)
```

**Structure Decision**: Distributed microservices with Aspire orchestration. Two new projects added:
1. **HookVerse.AppHost**: Aspire orchestrator that defines service topology, container resources, and dependencies
2. **HookVerse.ServiceDefaults**: Shared library that all services reference for automatic service discovery, health checks, and OpenTelemetry configuration

Existing 6 .NET projects remain unchanged in structure but will add references to ServiceDefaults and update their Program.cs to call `builder.AddServiceDefaults()` for Aspire integration.

## Complexity Tracking

*Fill ONLY if Constitution Check has violations that must be justified*

**No violations** - All constitution principles are satisfied. No complexity justifications required.

---

## Phase 0: Research (COMPLETED)

**Output**: `research.md` ✅

**Summary**: All technical unknowns resolved. Key decisions:
- R1: Aspire project structure (AppHost + ServiceDefaults)
- R2: Container resources via Aspire hosting packages
- R3: Azure managed services for production
- R4: Secret management (User Secrets local, Key Vault production)
- R5: Kubernetes manifest generation via `dotnet publish`
- R6: Azure Bicep generation via `dotnet publish`
- R7: Service discovery using Aspire built-in capabilities
- R8: OpenTelemetry with pluggable exporters
- R9: Health checks (liveness + readiness)
- R10: Testing strategy (startup, discovery, manifest validation tests)

**Next**: Proceed to Phase 1 (Design & Contracts).

---

## Phase 1: Design & Contracts (COMPLETED)

**Outputs**:
- `data-model.md` ✅
- `contracts/aspire-contracts.md` ✅
- `quickstart.md` ✅
- Agent context updated (GitHub Copilot) ✅

**Summary**:
- **Data Model**: Defined 4 configuration entities (AspireResourceDefinition, ServiceDependency, ManifestOutputConfiguration, ServiceDefaults). No changes to existing webhook domain model.
- **Contracts**: Defined 5 configuration contracts (AppHost topology, ServiceDefaults integration, Kubernetes manifests, Azure Bicep, CI/CD integration). No new REST APIs (infrastructure feature).
- **Quickstart**: Created comprehensive guide with 7 scenarios (local dev, debugging, K8s manifest generation, Azure Bicep generation, adding new service, troubleshooting, CI/CD).
- **Agent Context**: Updated `.github/copilot-instructions.md` with Aspire 9.5, .NET 10, PostgreSQL, Azure SQL technologies.

**Constitution Check Re-evaluation**: ✅ ALL PASS (including Test-First Development - test strategy defined in R10).

**Next**: Proceed to Phase 2 (Task Breakdown) via `/speckit.tasks` command.

---

## Phase 2: Task Breakdown

**Status**: NOT STARTED (run `/speckit.tasks` to generate tasks.md)

**Expected Output**: `tasks.md` with:
- Granular implementation tasks (2-4 hour units)
- Acceptance tests defined FIRST (per Constitution V)
- Dependencies between tasks
- Estimated effort
- Priority/ordering

**Next Command**: Follow instructions in `speckit.tasks.prompt.md` to generate task breakdown.

