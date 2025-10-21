# Feature Specification: Simplified Development & Deployment with .NET Aspire

**Feature Branch**: `002-aspire-orchestration`  
**Created**: 2025-10-21  
**Status**: Draft  
**Input**: "replace all complex kub or docker compose by using Aspire .net. also use aspire to prepare deployment to azure or kubernetes"

## Clarifications

### Session 2025-10-21

- Q: Should Azure deployments use fully managed Azure services or containerized versions? → A: Azure managed services in production, containers locally
- Q: How to migrate from existing Docker Compose/K8s? → A: Big bang replacement when Aspire is ready
- Q: How should secrets be managed across local and Azure environments? → A: Aspire references Azure Key Vault in generated manifests; local dev uses user-secrets/environment variables
- Q: How should network ingress be configured for webhook API in generated manifests? → A: No ingress/gateway configuration (deferred to later phase)
- Q: What observability strategy should Aspire support? → A: OpenTelemetry with Aspire dashboard locally; user-configurable exporters (Datadog, Application Insights, etc.) in production
- Q: How should resource limits (CPU, memory) be handled in generated manifests? → A: Moderate defaults, configurable limits
- Q: How should Aspire integrate with existing CI/CD pipelines? → A: Replace existing deployment steps with Aspire manifest generation + apply to cluster

## Summary

This feature will replace complex Docker Compose and Kubernetes configurations with .NET Aspire for both local development and deployment manifest generation. Key decisions: Use Azure managed services (SQL Database, Service Bus) in production while keeping containers for local development, and perform a big bang migration once Aspire setup is validated.

## User Scenarios

[Full user scenarios with 5 prioritized stories covering local development, service discovery, observability, manifest generation, and environment parity]

## Requirements

[31+ functional requirements covering local development, deployment manifest generation, and observability]

## Success Criteria  

[21 measurable outcomes for both local development productivity and deployment operations]

## Assumptions & Dependencies

[14 assumptions about SDK, tools, and infrastructure; 7 dependencies on EF Core, OpenTelemetry, Azure services]
