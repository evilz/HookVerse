# Quickstart Guide: HookVerse Development

**Feature**: 001-webhook-delivery-platform  
**Date**: 2025-10-18  
**Purpose**: Get HookVerse running locally for development

## Prerequisites

### Required Software

- **.NET 10 SDK**: [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Docker Desktop**: [Download](https://www.docker.com/products/docker-desktop)
- **Git**: [Download](https://git-scm.com/)
- **IDE**: Visual Studio 2025, VS Code, or JetBrains Rider

### Optional Tools

- **Postman** or **Insomnia**: For API testing
- **k9s** or **kubectl**: For Kubernetes development
- **Azure Data Studio** or **pgAdmin**: For database inspection

---

## Quick Start (5 minutes)

### 1. Clone Repository

```bash
git clone https://github.com/evilz/HookVerse.git
cd HookVerse
```

### 2. Start Dependencies with Docker Compose

```bash
cd docker
docker-compose up -d
```

This starts:
- **PostgreSQL** on port 5432 (default: SQLite for quick start)
- **RabbitMQ** on port 5672 (management UI on port 15672)
- **Redis** on port 6379 (optional, for rate limiting)

### 3. Run Database Migrations

```bash
dotnet ef database update --project src/HookVerse.Infrastructure --startup-project src/HookVerse.Api
```

### 4. Start Services

**Terminal 1 - API Service**:
```bash
cd src/HookVerse.Api
dotnet run
```

API available at: `https://localhost:5001`  
Swagger UI: `https://localhost:5001/swagger`

**Terminal 2 - Worker Service**:
```bash
cd src/HookVerse.Worker
dotnet run
```

**Terminal 3 - Dashboard**:
```bash
cd src/HookVerse.Dashboard
dotnet run
```

Dashboard available at: `https://localhost:5003`

### 5. Test the API

Send your first webhook:

```bash
curl -X POST https://localhost:5001/api/v1/webhooks \
  -H "Content-Type: application/json" \
  -H "X-API-Key: dev-api-key-12345" \
  -d '{
    "eventType": "order.created",
    "payload": {
      "orderId": "ORD-123",
      "amount": 99.99
    }
  }'
```

---

## Project Structure

```
HookVerse/
├── src/
│   ├── HookVerse.Api/           # REST API (port 5001)
│   ├── HookVerse.Core/          # Domain logic
│   ├── HookVerse.Infrastructure/# Data access, messaging
│   ├── HookVerse.Worker/        # Background worker (port 5002)
│   ├── HookVerse.Dashboard/     # Blazor UI (port 5003)
│   └── HookVerse.Shared/        # Shared contracts
├── tests/
│   ├── HookVerse.Api.Tests/
│   ├── HookVerse.Core.Tests/
│   ├── HookVerse.Integration.Tests/
│   └── HookVerse.Contract.Tests/
├── docs/
├── docker/
│   ├── docker-compose.yml
│   └── Dockerfile.*
└── specs/                       # Feature specifications
```

---

## Configuration

### appsettings.Development.json (API)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=hookverse;Username=postgres;Password=postgres"
  },
  "MessageBus": {
    "Provider": "RabbitMQ",
    "ConnectionString": "host=localhost;username=guest;password=guest"
  },
  "Redis": {
    "ConnectionString": "localhost:6379",
    "Enabled": false
  },
  "RateLimiting": {
    "RequestsPerMinute": 1000,
    "BurstSize": 2000
  },
  "Observability": {
    "ServiceName": "HookVerse.Api",
    "OtlpEndpoint": "http://localhost:4317"
  }
}
```

### Environment Variables

```bash
# Database
HOOKVERSE_DB_PROVIDER=PostgreSQL        # SQLite, PostgreSQL, SqlServer, MySQL
HOOKVERSE_DB_CONNECTION=<connection-string>

# Message Bus
HOOKVERSE_MESSAGEBUS_PROVIDER=RabbitMQ  # RabbitMQ, Kafka, AmazonSQS
HOOKVERSE_MESSAGEBUS_CONNECTION=<connection-string>

# Security
HOOKVERSE_API_KEY=<your-api-key>
HOOKVERSE_ENCRYPTION_KEY=<32-byte-base64-key>

# Observability
HOOKVERSE_OTEL_ENDPOINT=http://localhost:4317
HOOKVERSE_LOG_LEVEL=Information         # Debug, Information, Warning, Error
```

---

## Development Workflow

### Running Tests

**Unit Tests**:
```bash
dotnet test tests/HookVerse.Core.Tests
```

**Integration Tests** (requires Docker):
```bash
dotnet test tests/HookVerse.Integration.Tests
```

**Contract Tests**:
```bash
dotnet test tests/HookVerse.Contract.Tests
```

**All Tests with Coverage**:
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### Code Quality

**Linting**:
```bash
dotnet format --verify-no-changes
```

**Auto-fix formatting**:
```bash
dotnet format
```

### Database Migrations

**Create Migration**:
```bash
dotnet ef migrations add MigrationName --project src/HookVerse.Infrastructure --startup-project src/HookVerse.Api
```

**Apply Migration**:
```bash
dotnet ef database update --project src/HookVerse.Infrastructure --startup-project src/HookVerse.Api
```

**Rollback Migration**:
```bash
dotnet ef database update PreviousMigrationName --project src/HookVerse.Infrastructure --startup-project src/HookVerse.Api
```

---

## Development Scenarios

### Scenario 1: Test Webhook Delivery End-to-End

1. **Create Event Type**:
```bash
curl -X POST https://localhost:5001/api/v1/event-types \
  -H "X-API-Key: dev-api-key-12345" \
  -d '{"name": "test.event", "version": "1.0.0"}'
```

2. **Create Subscription**:
```bash
curl -X POST https://localhost:5001/api/v1/subscriptions \
  -H "X-API-Key: dev-api-key-12345" \
  -d '{
    "eventType": "test.event",
    "endpointUrl": "https://webhook.site/your-unique-url",
    "secret": "my-secret-key-must-be-at-least-32-chars-long"
  }'
```

3. **Send Webhook**:
```bash
curl -X POST https://localhost:5001/api/v1/webhooks \
  -H "X-API-Key: dev-api-key-12345" \
  -d '{
    "eventType": "test.event",
    "payload": {"message": "Hello, webhook!"}
  }'
```

4. **Check webhook.site for delivery**

---

### Scenario 2: Test Retry Logic

1. **Create mock endpoint that fails**:
```bash
curl -X POST https://localhost:5001/api/v1/mock-endpoints \
  -H "X-API-Key: dev-api-key-12345" \
  -d '{
    "name": "Failing Endpoint",
    "responseStatus": 500,
    "responseBody": "Internal Server Error"
  }'
```

2. **Create subscription to mock endpoint**

3. **Send webhook and observe retries in Dashboard**

---

### Scenario 3: Test Schema Validation

1. **Create event type with JSON Schema**:
```bash
curl -X POST https://localhost:5001/api/v1/event-types \
  -H "X-API-Key: dev-api-key-12345" \
  -d '{
    "name": "order.created",
    "version": "1.0.0"
  }'

curl -X PUT https://localhost:5001/api/v1/event-types/order.created/schema \
  -H "X-API-Key: dev-api-key-12345" \
  -d '{
    "format": "json_schema",
    "content": "{\"type\":\"object\",\"required\":[\"orderId\"],\"properties\":{\"orderId\":{\"type\":\"string\"}}}"
  }'
```

2. **Send valid webhook** (should succeed):
```bash
curl -X POST https://localhost:5001/api/v1/webhooks \
  -H "X-API-Key: dev-api-key-12345" \
  -d '{
    "eventType": "order.created",
    "payload": {"orderId": "ORD-123"}
  }'
```

3. **Send invalid webhook** (should fail validation):
```bash
curl -X POST https://localhost:5001/api/v1/webhooks \
  -H "X-API-Key: dev-api-key-12345" \
  -d '{
    "eventType": "order.created",
    "payload": {"amount": 99.99}
  }'
```

---

## Debugging

### Visual Studio 2025

1. Open `HookVerse.sln`
2. Set multiple startup projects:
   - HookVerse.Api
   - HookVerse.Worker
   - HookVerse.Dashboard
3. Press F5 to start debugging

### VS Code

1. Install C# Dev Kit extension
2. Open workspace
3. Use launch configuration:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Launch API",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/src/HookVerse.Api/bin/Debug/net10.0/HookVerse.Api.dll",
      "cwd": "${workspaceFolder}/src/HookVerse.Api",
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  ]
}
```

### Viewing Logs

**API Logs**:
```bash
docker logs hookverse-api -f
```

**Worker Logs**:
```bash
docker logs hookverse-worker -f
```

**RabbitMQ Management UI**:
- URL: http://localhost:15672
- Username: `guest`
- Password: `guest`

---

## Troubleshooting

### Port Already in Use

**Problem**: `Address already in use: 0.0.0.0:5001`

**Solution**: Change port in `appsettings.Development.json`:
```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5010"
      }
    }
  }
}
```

---

### Database Connection Failed

**Problem**: `Npgsql.NpgsqlException: Connection refused`

**Solution**: Ensure PostgreSQL is running:
```bash
docker-compose ps
docker-compose up -d postgres
```

Or switch to SQLite (no Docker required):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=hookverse.db"
  },
  "Database": {
    "Provider": "SQLite"
  }
}
```

---

### Message Bus Connection Failed

**Problem**: `MassTransit.RabbitMqTransport.RabbitMqConnectionException`

**Solution**: Ensure RabbitMQ is running:
```bash
docker-compose up -d rabbitmq
```

---

## Next Steps

1. **Read the spec**: `specs/001-webhook-delivery-platform/spec.md`
2. **Review data model**: `specs/001-webhook-delivery-platform/data-model.md`
3. **Explore API contracts**: `specs/001-webhook-delivery-platform/contracts/`
4. **Run integration tests**: Understand expected behavior
5. **Implement first user story**: See `tasks.md` (to be generated)

---

## Useful Commands

```bash
# Build all projects
dotnet build

# Run specific project
dotnet run --project src/HookVerse.Api

# Watch mode (auto-reload on code changes)
dotnet watch --project src/HookVerse.Api

# Clean build artifacts
dotnet clean

# Restore NuGet packages
dotnet restore

# List available migrations
dotnet ef migrations list --project src/HookVerse.Infrastructure

# Generate OpenAPI spec
dotnet run --project src/HookVerse.Api -- --generate-openapi

# Run with specific environment
ASPNETCORE_ENVIRONMENT=Staging dotnet run --project src/HookVerse.Api
```

---

## Docker Development

### Build Docker Images

```bash
# Build API image
docker build -f docker/Dockerfile.api -t hookverse/api:dev .

# Build Worker image
docker build -f docker/Dockerfile.worker -t hookverse/worker:dev .

# Build Dashboard image
docker build -f docker/Dockerfile.dashboard -t hookverse/dashboard:dev .
```

### Run Full Stack with Docker Compose

```bash
docker-compose -f docker/docker-compose.yml -f docker/docker-compose.dev.yml up
```

---

## Resources

- **API Documentation**: http://localhost:5001/swagger
- **RabbitMQ Management**: http://localhost:15672
- **Dashboard**: http://localhost:5003
- **OpenTelemetry Collector**: http://localhost:4317 (gRPC)
- **Prometheus**: http://localhost:9090 (if enabled)
- **Jaeger UI**: http://localhost:16686 (if enabled)

---

## Contributing

See `CONTRIBUTING.md` for guidelines on:
- Code style
- Commit messages
- Pull request process
- Testing requirements

---

## Getting Help

- **GitHub Issues**: https://github.com/evilz/HookVerse/issues
- **Discussions**: https://github.com/evilz/HookVerse/discussions
- **Documentation**: https://hookverse.io/docs
