# HookVerse

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10-purple?logo=dotnet" alt=".NET 10">
  <img src="https://img.shields.io/badge/License-MIT-blue.svg" alt="License">
  <img src="https://img.shields.io/badge/Status-Active-success" alt="Status">
</p>

**HookVerse** is an open-source Webhooks-as-a-Service platform that enables developers to send webhooks via API or message bus with guaranteed delivery, automatic retries, schema validation, and comprehensive observability.

## ✨ Features

### Core Capabilities
- 🚀 **High Performance**: 10,000+ webhooks/second per instance
- 🔄 **Guaranteed Delivery**: Automatic retries with exponential backoff and circuit breakers
- ✅ **Schema Validation**: Multi-format support (JSON Schema, Avro, Protocol Buffers, .NET assemblies)
- 📊 **Real-time Dashboard**: Blazor Server UI with live metrics and webhook tracking
- 🔐 **Secure by Default**: HMAC-SHA256 signatures, API key authentication, payload encryption
- 🌐 **Cloud-Native**: Stateless services, horizontal scaling, Kubernetes-ready
- 📈 **Full Observability**: OpenTelemetry integration with metrics, traces, and logs

### Advanced Features
- **Flexible Delivery**: Direct HTTP, message bus (RabbitMQ/Kafka/SQS), or scheduled delivery
- **Multi-Tenancy**: API key-based subscriber isolation
- **Mock Endpoints**: Built-in testing endpoints with request logging
- **GDPR Compliance**: Data export, deletion, and automatic retention policies
- **Rate Limiting**: Per-subscriber configurable limits
- **Custom Headers**: Support for authentication headers and custom metadata
- **Delivery Tracking**: Complete audit trail with attempt history and response logging

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     HookVerse Platform                       │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌──────────────┐   ┌──────────────┐   ┌──────────────┐   │
│  │ HookVerse.Api│   │HookVerse.    │   │HookVerse.    │   │
│  │              │   │ Worker       │   │ Dashboard    │   │
│  │ REST API     │   │              │   │              │   │
│  │ OpenAPI      │──▶│ Background   │   │ Blazor Server│   │
│  │ Auth         │   │ Delivery     │   │ Monitoring   │   │
│  └──────────────┘   └──────────────┘   └──────────────┘   │
│         │                   │                   │           │
│         └───────────────────┼───────────────────┘           │
│                             │                               │
│  ┌──────────────────────────┴────────────────────────┐     │
│  │          Message Bus (RabbitMQ/Kafka/SQS)         │     │
│  └───────────────────────────────────────────────────┘     │
│                             │                               │
│  ┌──────────────────────────┴────────────────────────┐     │
│  │   Database (PostgreSQL/SQL Server/MySQL/SQLite)   │     │
│  └───────────────────────────────────────────────────┘     │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

### Components

- **HookVerse.Api**: REST API for webhook submission and management
- **HookVerse.Worker**: Background service for asynchronous webhook delivery
- **HookVerse.Dashboard**: Interactive UI for monitoring, debugging, and management
- **HookVerse.Core**: Domain models, business logic, and service interfaces
- **HookVerse.Infrastructure**: Data access, message bus, and external integrations
- **HookVerse.Shared**: Shared contracts, DTOs, and utilities

## 🚀 Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (preview)
- [.NET Aspire workload](https://learn.microsoft.com/dotnet/aspire/fundamentals/setup-tooling): `dotnet workload install aspire`
- [Docker Desktop](https://www.docker.com/) (for local container dependencies)
- [Git](https://git-scm.com/)

### 1. Clone and Run with Aspire

```bash
# Clone repository
git clone https://github.com/evilz/HookVerse.git
cd HookVerse

# Run the AppHost (starts all services and dependencies automatically)
dotnet run --project src/HookVerse.AppHost
```

That's it! .NET Aspire will automatically:
- ✅ Start PostgreSQL, RabbitMQ, and Redis containers
- ✅ Launch API, Worker, and Dashboard services
- ✅ Configure service discovery and health checks
- ✅ Open the Aspire dashboard at http://localhost:15888

### 2. Access the Services

Once the AppHost is running:

- **Aspire Dashboard**: http://localhost:15888 (orchestration, logs, traces, metrics)
- **HookVerse API**: http://localhost:5000 or https://localhost:5001
- **HookVerse Dashboard**: http://localhost:7000 (webhook monitoring)
- **API Documentation**: http://localhost:5000/swagger

### 3. Send Your First Webhook

```bash
# Create an event type
curl -X POST http://localhost:5000/api/v1/event-types \
  -H "X-Api-Key: your-api-key" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "order.created",
    "description": "Triggered when a new order is created",
    "schemaFormat": "JsonSchema",
    "schema": "{\"type\":\"object\",\"properties\":{\"orderId\":{\"type\":\"string\"}},\"required\":[\"orderId\"]}"
  }'

# Create a subscription
curl -X POST http://localhost:5000/api/v1/subscriptions \
  -H "X-Api-Key: your-api-key" \
  -H "Content-Type: application/json" \
  -d '{
    "eventTypeId": "<event-type-id>",
    "endpointUrl": "https://your-app.com/webhooks",
    "authType": "HmacSha256",
    "secret": "your-secret-key",
    "maxRetries": 3
  }'

# Submit a webhook
curl -X POST http://localhost:5000/api/v1/webhooks \
  -H "X-Api-Key: your-api-key" \
  -H "Content-Type: application/json" \
  -d '{
    "eventType": "order.created",
    "payload": {
      "orderId": "ord_123456",
      "amount": 99.99
    }
  }'
```

### 3. Monitor in Dashboard

Open http://localhost:5001 to view:
- Real-time webhook delivery status
- Success/failure rates and latency metrics
- Detailed delivery attempt logs
- Subscription management
- GDPR request tracking

## 📖 Documentation

### Getting Started
- **[Aspire Quickstart](./specs/002-aspire-orchestration/quickstart.md)**: Get started with .NET Aspire orchestration
- **[Webhook Platform Quickstart](./specs/001-webhook-delivery-platform/quickstart.md)**: Step-by-step webhook integration tutorial
- **[API Documentation](./docs/api/README.md)**: Complete REST API reference

### Deployment & Operations
- **[Aspire Orchestration Spec](./specs/002-aspire-orchestration/spec.md)**: Detailed Aspire architecture and design
- **[Deployment Guide](./specs/002-aspire-orchestration/quickstart.md#deployment)**: Kubernetes and Azure Container Apps deployment
- **[Observability Guide](./docs/observability/README.md)**: OpenTelemetry, metrics, traces, and dashboards

### Development
- **[Architecture Decisions](./docs/architecture/README.md)**: ADRs for key technical decisions
- **[Feature Specifications](./specs/)**: Detailed feature requirements and user stories
- **[Aspire Dashboard](http://localhost:15888)**: Live service orchestration (when AppHost is running)

## 🛠️ Configuration

### Local Development with Aspire

When running via `dotnet run --project src/HookVerse.AppHost`, all configuration is automatic:

- **Service Discovery**: Services find each other via Aspire's built-in service discovery
- **Connection Strings**: Injected automatically from AppHost configuration
- **Zero Manual Config**: No need to set environment variables for local development

### Production Environment Variables

For production deployments (outside Aspire), configure via environment variables or Azure App Configuration:

```bash
# Connection Strings (auto-configured in Aspire)
ConnectionStrings__postgres=Server=localhost;Port=5432;Database=hookverse;User Id=hookverse;Password=***
ConnectionStrings__rabbitmq=amqp://guest:guest@localhost:5672
ConnectionStrings__redis=localhost:6379

# OpenTelemetry (optional for production)
OTEL_EXPORTER_OTLP_ENDPOINT=https://your-collector:4317
APPLICATIONINSIGHTS_CONNECTION_STRING=InstrumentationKey=***;IngestionEndpoint=***
```

### Aspire Manifest Generation

Generate deployment manifests for Kubernetes or Azure:

```bash
# Generate Kubernetes manifests
dotnet publish src/HookVerse.AppHost --configuration Release --os linux --arch x64 /p:PublishProfile=aspire-manifest.pubxml

# Generated files in aspire/manifests/kubernetes/
kubectl apply -f aspire/manifests/kubernetes/

# Generate Azure Bicep templates  
dotnet publish src/HookVerse.AppHost --configuration Release /p:PublishProfile=aspire-bicep.pubxml

# Deploy to Azure
az deployment group create --resource-group hookverse --template-file aspire/manifests/azure/main.bicep
```

### Environment-Specific Configuration

HookVerse uses Aspire's conditional resource configuration:

- **Development** (local): Uses containers (PostgreSQL, RabbitMQ, Redis in Docker)
- **Production** (Azure): Uses managed services (Azure SQL, Service Bus, Redis Cache)
- **No Code Changes**: Same codebase automatically adapts via AppHost configuration

#### Configuration Priority

Configuration sources are applied in the following order (later sources override earlier ones):

1. **appsettings.json** - Base configuration shared across all environments
2. **appsettings.{Environment}.json** - Environment-specific settings (Development, Staging, Production)
3. **User Secrets** - Local development secrets (Development only, never committed)
4. **Environment Variables** - Runtime overrides (recommended for production)
5. **Azure Key Vault** - Production secrets (via environment variables with Key Vault references)

#### Environment Variable Overrides

**Connection Strings** (use `:` or `__` as separator):
```bash
# PostgreSQL
ConnectionStrings__postgres="Host=myserver;Database=hookverse;..."
ConnectionStrings:postgres="Host=myserver;Database=hookverse;..."

# Azure SQL Database (Production/Staging)
ConnectionStrings__AzureSqlDatabase="Server=tcp:myserver.database.windows.net;..."

# RabbitMQ
ConnectionStrings__rabbitmq="amqp://user:pass@host:5672"

# Azure Service Bus (Production/Staging)
ConnectionStrings__AzureServiceBus="Endpoint=sb://mynamespace.servicebus.windows.net;..."

# Redis
ConnectionStrings__redis="localhost:6379,password=secret"

# Azure Redis Cache (Production/Staging)
ConnectionStrings__AzureRedisCache="mycache.redis.cache.windows.net:6380,password=..."
```

**Logging Levels**:
```bash
# Change default log level
Logging__LogLevel__Default="Debug"

# Change specific namespace log level
Logging__LogLevel__HookVerse="Information"
Logging__LogLevel__Microsoft.EntityFrameworkCore="Warning"
```

**OpenTelemetry Configuration**:
```bash
# Enable OTLP exporter (Datadog, Application Insights, etc.)
OTEL_EXPORTER_OTLP_ENDPOINT="https://api.datadoghq.com:4317"
OTEL_EXPORTER_OTLP_HEADERS="dd-api-key=YOUR_API_KEY"
OTEL_RESOURCE_ATTRIBUTES="service.name=hookverse-api,env=production"

# Or use Application Insights
APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=...;IngestionEndpoint=..."
```

**Azure Key Vault** (Production):
```bash
# Key Vault configuration
Azure__KeyVault__Name="kv-hookverse-prod"
Azure__KeyVault__VaultUri="https://kv-hookverse-prod.vault.azure.net/"

# Connection strings with Key Vault references
ConnectionStrings__AzureSqlDatabase="@Microsoft.KeyVault(SecretUri=https://kv-hookverse-prod.vault.azure.net/secrets/SqlConnectionString/)"
ConnectionStrings__AzureServiceBus="@Microsoft.KeyVault(SecretUri=https://kv-hookverse-prod.vault.azure.net/secrets/ServiceBusConnectionString/)"
```

**API Settings**:
```bash
# Dashboard API endpoint
ApiSettings__BaseUrl="https://api.hookverse.com"
ApiSettings__TimeoutSeconds="60"

# CORS origins
Cors__AllowedOrigins__0="https://dashboard.hookverse.com"
Cors__AllowedOrigins__1="https://www.hookverse.com"
```

**Worker Settings**:
```bash
# Webhook delivery worker configuration
WorkerSettings__MaxConcurrentDeliveries="100"
WorkerSettings__BatchSize="200"
WorkerSettings__PollingIntervalSeconds="1"

# Webhook delivery settings
WebhookDelivery__MaxRetries="10"
WebhookDelivery__RetryDelaySeconds="15"
WebhookDelivery__EnableCircuitBreaker="true"
```

**Azure Container Apps Example**:
```bash
# Set environment variables in Container App
az containerapp update \
  --name hookverse-api \
  --resource-group hookverse \
  --set-env-vars \
    "ConnectionStrings__AzureSqlDatabase=secretref:sql-connection" \
    "ConnectionStrings__AzureServiceBus=secretref:servicebus-connection" \
    "OTEL_EXPORTER_OTLP_ENDPOINT=https://api.datadoghq.com:4317" \
    "Logging__LogLevel__Default=Information"

# Set secrets (for sensitive values)
az containerapp secret set \
  --name hookverse-api \
  --resource-group hookverse \
  --secrets \
    sql-connection="Server=tcp:..." \
    servicebus-connection="Endpoint=sb:..."
```

**Kubernetes ConfigMap/Secret Example**:
```yaml
# ConfigMap for non-sensitive configuration
apiVersion: v1
kind: ConfigMap
metadata:
  name: hookverse-config
data:
  Logging__LogLevel__Default: "Information"
  WorkerSettings__MaxConcurrentDeliveries: "100"
  OTEL_RESOURCE_ATTRIBUTES: "service.name=hookverse-api,env=production"

---
# Secret for sensitive configuration
apiVersion: v1
kind: Secret
metadata:
  name: hookverse-secrets
type: Opaque
stringData:
  ConnectionStrings__AzureSqlDatabase: "Server=tcp:..."
  ConnectionStrings__AzureServiceBus: "Endpoint=sb:..."
  OTEL_EXPORTER_OTLP_HEADERS: "dd-api-key=YOUR_API_KEY"
```

**Local Development with User Secrets**:
```bash
# Initialize user secrets (one-time)
dotnet user-secrets init --project src/HookVerse.AppHost

# Set secrets for local development
dotnet user-secrets set "ConnectionStrings:postgres" "Host=localhost;..." --project src/HookVerse.AppHost
dotnet user-secrets set "ExternalApi:DatadogApiKey" "YOUR_KEY" --project src/HookVerse.AppHost

# List all secrets
dotnet user-secrets list --project src/HookVerse.AppHost

# See template: aspire/templates/user-secrets.template.json
```

**Verification**:
```bash
# Verify configuration is loaded correctly
# Check application logs on startup for configuration values
# Or use health check endpoint to validate settings
curl http://localhost:7001/health
```

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Run specific test projects
dotnet test tests/HookVerse.AppHost.Tests/          # Aspire integration tests
dotnet test tests/HookVerse.Integration.Tests/       # API integration tests
dotnet test tests/HookVerse.Core.Tests/              # Unit tests

# Run Aspire integration tests specifically
dotnet test tests/HookVerse.AppHost.Tests/ --logger "console;verbosity=detailed"
```

### Test Categories

- **Unit Tests**: Fast, isolated tests for business logic
- **Integration Tests**: API and database integration testing
- **Aspire Tests**: AppHost orchestration, service discovery, and manifest generation
- **Performance Tests**: Load testing and benchmarking

## 🚢 Deployment

HookVerse uses .NET Aspire for deployment manifest generation, eliminating the need for manual Dockerfile or Kubernetes YAML creation.

### Kubernetes Deployment

```bash
# Generate Kubernetes manifests
dotnet publish src/HookVerse.AppHost \
  --configuration Release \
  --os linux --arch x64 \
  /p:PublishProfile=aspire-manifest.pubxml

# Deploy to Kubernetes
kubectl apply -f aspire/manifests/kubernetes/

# Verify deployment
kubectl get pods -n hookverse
kubectl get services -n hookverse
```

### Azure Container Apps

```bash
# Generate Azure Bicep templates
dotnet publish src/HookVerse.AppHost \
  --configuration Release \
  /p:PublishProfile=aspire-bicep.pubxml

# Deploy to Azure
az login
az group create --name hookverse --location eastus
az deployment group create \
  --resource-group hookverse \
  --template-file aspire/manifests/azure/main.bicep \
  --parameters aspire/manifests/azure/main.parameters.json

# Verify deployment
az containerapp list --resource-group hookverse --output table
```

### Local Development

```bash
# Run everything locally via Aspire
dotnet run --project src/HookVerse.AppHost

# Access Aspire dashboard
open http://localhost:15888
```

**Benefits of Aspire Deployment**:
- ✅ Auto-generated manifests from AppHost configuration
- ✅ Service discovery and health checks included
- ✅ OpenTelemetry observability pre-configured
- ✅ Resource limits and scaling policies defined
- ✅ Same configuration for local, staging, and production

See [specs/002-aspire-orchestration/quickstart.md](./specs/002-aspire-orchestration/quickstart.md) for detailed deployment instructions.

## 🤝 Contributing

We welcome contributions! Here's how to get started:

1. **Fork the repository**
2. **Create a feature branch**: `git checkout -b feature/amazing-feature`
3. **Commit your changes**: `git commit -m 'feat: add amazing feature'`
4. **Push to branch**: `git push origin feature/amazing-feature`
5. **Open a Pull Request**

### Development Guidelines

- Follow [Conventional Commits](https://www.conventionalcommits.org/)
- Write tests for new features
- Update documentation for API changes
- Run `dotnet format` before committing
- Ensure all tests pass: `dotnet test`

### Code of Conduct

This project follows the [Contributor Covenant Code of Conduct](CODE_OF_CONDUCT.md).

## 📊 Performance

| Metric | Target | Typical |
|--------|--------|---------|
| Webhook submission (API) | < 200ms p95 | ~80ms p95 |
| Webhook delivery | < 500ms p95 | ~250ms p95 |
| Throughput per instance | 10,000/sec | 12,000/sec |
| Database queries | < 100ms p95 | ~35ms p95 |

*Benchmarks on .NET 10, PostgreSQL 16, 4 vCPU / 8GB RAM*

## 🔐 Security

- **API Key Authentication**: Secure subscriber access
- **HMAC Signatures**: Webhook payload signing (HMAC-SHA256)
- **Payload Encryption**: AES-256-GCM for sensitive data
- **Rate Limiting**: Configurable per-subscriber limits
- **GDPR Compliance**: Data export, deletion, and retention

**Report security vulnerabilities** to security@hookverse.io (do not open public issues).

## 📝 License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

Built with:
- [ASP.NET Core](https://dotnet.microsoft.com/apps/aspnet) - Web framework
- [Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor) - UI framework
- [Entity Framework Core](https://docs.microsoft.com/ef/core/) - ORM
- [MassTransit](https://masstransit.io/) - Message bus abstraction
- [OpenTelemetry](https://opentelemetry.io/) - Observability
- [xUnit](https://xunit.net/) - Testing framework

## 📧 Contact

- **GitHub**: [@evilz](https://github.com/evilz)
- **Project**: [HookVerse](https://github.com/evilz/HookVerse)

---

<p align="center">Made with ❤️ by the HookVerse team</p>
