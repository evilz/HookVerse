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
- [Docker](https://www.docker.com/) & Docker Compose
- [Git](https://git-scm.com/)

### 1. Clone and Start

```bash
# Clone repository
git clone https://github.com/evilz/HookVerse.git
cd HookVerse

# Start dependencies (PostgreSQL + RabbitMQ)
docker-compose up -d

# Run database migrations
dotnet ef database update --project src/HookVerse.Infrastructure

# Start all services
dotnet run --project src/HookVerse.Api          # API on http://localhost:5000
dotnet run --project src/HookVerse.Worker        # Worker (background)
dotnet run --project src/HookVerse.Dashboard     # Dashboard on http://localhost:5001
```

### 2. Send Your First Webhook

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

- **[Quickstart Guide](./specs/001-webhook-delivery-platform/quickstart.md)**: Step-by-step integration tutorial
- **[API Documentation](./docs/api/README.md)**: Complete REST API reference
- **[Deployment Guide](./docs/deployment/kubernetes.md)**: Kubernetes & Helm deployment
- **[Architecture Decisions](./docs/architecture/README.md)**: ADRs for key technical decisions
- **[Feature Specification](./specs/001-webhook-delivery-platform/spec.md)**: Detailed feature requirements

## 🛠️ Configuration

### Environment Variables

```bash
# Database (PostgreSQL example)
Database__Type=PostgreSQL
Database__Host=localhost
Database__Port=5432
Database__Database=hookverse
Database__Username=hookverse
Database__Password=your-password

# Message Bus (RabbitMQ example)
MessageBus__Transport=RabbitMQ
MessageBus__Host=localhost
MessageBus__Port=5672
MessageBus__Username=guest
MessageBus__Password=guest

# OpenTelemetry
OpenTelemetry__Enabled=true
OpenTelemetry__OtlpEndpoint=http://localhost:4317
```

### Database Support

- **PostgreSQL** (recommended for production)
- **SQL Server**
- **MySQL**
- **SQLite** (development/testing)

### Message Bus Support

- **RabbitMQ** (default)
- **Apache Kafka**
- **AWS SQS**
- **Azure Service Bus**

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Run integration tests only
dotnet test --filter Category=Integration
```

## 🚢 Deployment

### Docker

```bash
# Build images
docker build -t hookverse/api -f src/HookVerse.Api/Dockerfile .
docker build -t hookverse/worker -f src/HookVerse.Worker/Dockerfile .
docker build -t hookverse/dashboard -f src/HookVerse.Dashboard/Dockerfile .

# Run with Docker Compose
docker-compose up -d
```

### Kubernetes

```bash
# Using Helm
helm install hookverse ./docs/deployment/helm/hookverse \
  --namespace hookverse \
  --create-namespace

# Or using raw manifests
kubectl apply -f docs/deployment/k8s/
```

See [Deployment Guide](./docs/deployment/kubernetes.md) for complete instructions.

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
