# Research: HookVerse - Webhook Delivery Platform

**Feature**: 001-webhook-delivery-platform  
**Date**: 2025-10-18  
**Purpose**: Technical research and architectural decisions for implementing the webhook delivery platform

## Technology Stack Decisions

### Decision 1: .NET 10 as Primary Runtime

**Decision**: Use .NET 10 (C#) for all services (API, Worker, Dashboard)

**Rationale**:
- **Performance**: .NET 10 achieves high throughput (10K+ requests/sec) with minimal GC pressure
- **Cross-platform**: Runs on Linux containers, Windows, macOS
- **Modern async/await**: Built-in support for async I/O operations critical for webhook delivery
- **Ecosystem**: Rich package ecosystem (EF Core, MassTransit, OpenTelemetry)
- **AOT compilation**: Ahead-of-time compilation available for reduced startup time
- **Long-term support**: .NET 10 LTS release with extended support lifecycle

**Alternatives Considered**:
- **Node.js**: Rejected due to less robust typing, harder to achieve consistent performance under load
- **Go**: Rejected due to less mature ORM options, smaller ecosystem for schema validation
- **Python**: Rejected due to GIL limitations for concurrent webhook delivery, slower performance
- **Java**: Rejected due to higher memory footprint, more verbose code

---

### Decision 2: EF Core with Provider Abstraction

**Decision**: Entity Framework Core 10 with pluggable database providers (SQLite default, PostgreSQL/SQL Server/MySQL supported)

**Rationale**:
- **Database agnostic**: Users can choose database based on scale and infrastructure
- **Migrations**: Code-first migrations for schema versioning
- **LINQ queries**: Type-safe queries with compile-time checking
- **Change tracking**: Automatic state management for entities
- **Relationship management**: Navigation properties simplify complex queries
- **Performance**: Compiled queries, split queries, and query caching available

**Implementation Strategy**:
- Abstract repositories behind `IRepository<T>` interfaces
- Use `IDbContextFactory<HookVerseDbContext>` for worker services
- Configure providers via `appsettings.json` with connection string + provider type
- SQLite for quick start and testing, PostgreSQL recommended for production

**Alternatives Considered**:
- **Dapper**: Rejected due to lack of migrations, more boilerplate code
- **Direct ADO.NET**: Rejected due to excessive boilerplate, no abstraction
- **NHibernate**: Rejected due to steeper learning curve, less .NET Core integration

---

### Decision 3: MassTransit for Message Bus Abstraction

**Decision**: MassTransit as abstraction layer over RabbitMQ, Kafka, and AWS SQS

**Rationale**:
- **Transport agnostic**: Single API supports RabbitMQ, Kafka, Amazon SQS, Azure Service Bus
- **Reliable messaging**: Built-in retry policies, circuit breakers, dead-letter queues
- **Message patterns**: Request-response, publish-subscribe, saga orchestration
- **Observability**: OpenTelemetry integration, built-in health checks
- **Configuration-based**: Switch transports via configuration without code changes
- **Consumer concurrency**: Automatic scaling of message consumers

**Transport Selection**:
- **RabbitMQ**: Default for most deployments (mature, feature-rich, easy operations)
- **Kafka**: For ultra-high throughput (100K+ webhooks/sec), event streaming use cases
- **AWS SQS**: For AWS-native deployments, serverless architectures

**Alternatives Considered**:
- **Direct RabbitMQ client**: Rejected due to no abstraction, harder to test
- **NServiceBus**: Rejected due to commercial licensing requirements
- **CAP**: Rejected due to less mature, smaller community

---

### Decision 4: Blazor Server for Dashboard

**Decision**: Blazor Server for subscriber portal and administration dashboard

**Rationale**:
- **Real-time updates**: SignalR-based server rendering enables live webhook log updates
- **Code sharing**: Share domain models between backend and frontend (C# end-to-end)
- **Component model**: Reusable UI components with two-way data binding
- **Responsive**: Bootstrap/Tailwind CSS integration for mobile-first design
- **Authentication**: Seamless integration with ASP.NET Core Identity
- **Performance**: Server-side rendering reduces client payload, faster initial load

**Alternatives Considered**:
- **Blazor WebAssembly**: Rejected due to larger download size, offline requirements not needed
- **React/Vue**: Rejected due to context switching between C# and JavaScript, duplicate models
- **Razor Pages**: Rejected due to lack of component reusability, less interactive

---

### Decision 5: Multi-Format Schema Support

**Decision**: Support JSON Schema, Avro, Protobuf, and .NET assembly schemas

**Rationale**:
- **JSON Schema**: De facto standard for JSON validation, human-readable, widely supported
- **Avro**: Efficient binary serialization, schema evolution support, popular in Kafka ecosystems
- **Protobuf**: High performance binary format, strong typing, popular in gRPC/microservices
- **NET Assemblies**: Allow users to provide C# classes for validation, maximum type safety

**Implementation Strategy**:
```csharp
public interface ISchemaValidator {
    Task<ValidationResult> ValidateAsync(string payload, SchemaDefinition schema);
}

// Implementations: JsonSchemaValidator, AvroSchemaValidator, 
// ProtobufSchemaValidator, AssemblySchemaValidator
```

**Validation Pipeline**:
1. Detect schema format from event type configuration
2. Load appropriate validator via dependency injection
3. Validate payload before queueing for delivery
4. Return detailed validation errors if invalid

**Alternatives Considered**:
- **JSON Schema only**: Rejected due to lack of binary format support
- **Custom schema language**: Rejected due to reinventing the wheel

---

### Decision 6: Polly for Resilience Policies

**Decision**: Use Polly for retry, circuit breaker, and timeout policies

**Rationale**:
- **Declarative policies**: Define resilience patterns in configuration
- **Retry with backoff**: Exponential backoff (1s, 5s, 25s, 2m, 10m) configurable
- **Circuit breaker**: Prevent cascading failures when subscriber endpoints down
- **Timeout policies**: Prevent webhook deliveries from hanging indefinitely
- **Telemetry**: Integrates with OpenTelemetry for tracking policy executions

**Configuration Example**:
```csharp
services.AddHttpClient("WebhookDelivery")
    .AddPolicyHandler(Policy
        .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
        .WaitAndRetryAsync(5, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))))
    .AddPolicyHandler(Policy
        .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
        .CircuitBreakerAsync(5, TimeSpan.FromMinutes(1)));
```

**Alternatives Considered**:
- **Manual retry logic**: Rejected due to error-prone, hard to test
- **Custom resilience library**: Rejected due to reinventing battle-tested patterns

---

### Decision 7: OpenTelemetry for Observability

**Decision**: OpenTelemetry .NET SDK for distributed tracing, logging, and metrics

**Rationale**:
- **Vendor neutral**: Export to Prometheus, Jaeger, Zipkin, Application Insights, etc.
- **Automatic instrumentation**: ASP.NET Core, HttpClient, EF Core auto-instrumented
- **Distributed tracing**: Trace webhook delivery across API → queue → worker → subscriber
- **Metrics**: Expose counters, histograms, gauges for delivery rates and latency
- **Structured logging**: Integrate with Serilog for rich log context

**Key Traces**:
- `webhook.received` → `webhook.queued` → `webhook.delivering` → `webhook.delivered`
- Span attributes: event type, subscriber ID, attempt number, response status

**Metrics**:
- `hookverse_webhook_delivery_duration_ms` (histogram)
- `hookverse_webhook_deliveries_total` (counter)
- `hookverse_webhook_delivery_failures_total` (counter)
- `hookverse_queue_depth` (gauge)

**Alternatives Considered**:
- **Application Insights only**: Rejected due to vendor lock-in
- **Serilog only**: Rejected due to lack of distributed tracing

---

### Decision 8: HMAC-SHA256 for Webhook Signatures

**Decision**: Generate HMAC-SHA256 signatures for all webhook deliveries

**Rationale**:
- **Industry standard**: GitHub, Stripe, Twilio all use HMAC-SHA256
- **Tamper-proof**: Recipients can verify payload integrity
- **Secret-based**: Shared secret between sender and subscriber
- **Efficient**: Fast computation, small signature size (32 bytes hex = 64 chars)

**Implementation**:
```csharp
public class WebhookSignatureService {
    public string GenerateSignature(string payload, string secret) {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return $"sha256={Convert.ToHexString(hash).ToLower()}";
    }
}
```

**HTTP Header**: `X-HookVerse-Signature: sha256=<hex_digest>`

**Alternatives Considered**:
- **RSA signatures**: Rejected due to complexity, slower performance
- **JWT**: Rejected due to overhead, not standard for webhooks

---

### Decision 9: Rate Limiting Strategy

**Decision**: ASP.NET Core rate limiter middleware with Redis-backed distributed cache

**Rationale**:
- **Built-in middleware**: .NET 10 includes rate limiting middleware
- **Flexible policies**: Fixed window, sliding window, token bucket, concurrency
- **Distributed**: Redis cache ensures rate limits work across multiple API instances
- **Per-sender limits**: Track rate limits by API key or tenant ID
- **Graceful**: Return `429 Too Many Requests` with `Retry-After` header

**Configuration**:
- Default: 1000 requests/minute per sender
- Burst: Allow temporary spikes up to 2000 requests/minute
- Custom limits: Per-tenant limits configurable in database

**Alternatives Considered**:
- **Custom middleware**: Rejected due to complexity, reinventing the wheel
- **Gateway-level rate limiting**: Rejected due to lack of per-tenant customization

---

### Decision 10: TestContainers for Integration Tests

**Decision**: TestContainers.NET for spinning up dependencies in integration tests

**Rationale**:
- **Real dependencies**: Test against real PostgreSQL, RabbitMQ, Redis instances
- **Isolated tests**: Each test gets fresh containers, no state leakage
- **CI/CD friendly**: Containers automatically cleaned up after tests
- **Docker-based**: Works anywhere Docker runs
- **Multiple services**: Compose multiple containers (database + message broker)

**Test Structure**:
```csharp
public class WebhookDeliveryTests : IAsyncLifetime {
    private PostgreSqlContainer _dbContainer;
    private RabbitMqContainer _mqContainer;
    
    public async Task InitializeAsync() {
        _dbContainer = new PostgreSqlBuilder().Build();
        _mqContainer = new RabbitMqBuilder().Build();
        await Task.WhenAll(_dbContainer.StartAsync(), _mqContainer.StartAsync());
    }
}
```

**Alternatives Considered**:
- **In-memory databases**: Rejected due to behavioral differences from real databases
- **Shared test databases**: Rejected due to test isolation issues

---

## Architecture Patterns

### Pattern 1: Clean Architecture

**Layers**:
1. **Core**: Domain entities, business logic, interfaces (no external dependencies)
2. **Infrastructure**: EF Core, MassTransit, HTTP clients (implements Core interfaces)
3. **API/Worker/Dashboard**: Entry points, controllers, consumers (orchestrate Core + Infrastructure)

**Benefits**:
- **Testability**: Core layer has no dependencies, easy to unit test
- **Flexibility**: Swap EF Core for Dapper without changing Core
- **Maintainability**: Clear separation of concerns

---

### Pattern 2: Repository Pattern

**Rationale**: Abstract EF Core behind repositories to enable testing and potential database swaps

**Interface**:
```csharp
public interface IWebhookEventRepository {
    Task<WebhookEvent> GetByIdAsync(Guid id);
    Task<IEnumerable<WebhookEvent>> GetBySubscriberAsync(Guid subscriberId, 
        DateTime? from, DateTime? to, int page, int pageSize);
    Task AddAsync(WebhookEvent webhookEvent);
    Task UpdateAsync(WebhookEvent webhookEvent);
}
```

---

### Pattern 3: Outbox Pattern

**Rationale**: Ensure webhook events are durably persisted before publishing to message bus

**Flow**:
1. API receives webhook → Saves to `webhook_events` table + `outbox` table in single transaction
2. Background processor reads `outbox` → Publishes to MassTransit → Marks outbox entry processed
3. Guarantees at-least-once delivery even if message bus is down

---

### Pattern 4: Background Service for Delivery

**Rationale**: Decouple webhook receipt from delivery for scale and resilience

**Components**:
- **API service**: Receives webhooks, validates, persists to database, publishes to queue
- **Worker service**: Consumes queue, delivers to subscribers, handles retries
- **Dead-letter handler**: Separate consumer for permanently failed webhooks

**Scaling**: Deploy multiple worker instances, MassTransit distributes load automatically

---

## Performance Optimizations

### Optimization 1: Async I/O Throughout

- All database queries use `async`/`await`
- All HTTP webhook deliveries use `HttpClient` with async methods
- MassTransit consumers are async by default
- Avoids thread pool starvation under high load

---

### Optimization 2: Connection Pooling

- **EF Core**: Connection pooling enabled by default
- **HttpClient**: Use `IHttpClientFactory` to reuse connections
- **MassTransit**: Connection pools for message broker

---

### Optimization 3: Compiled Queries

- Use EF Core compiled queries for frequently executed reads
- Cache schema validators in memory (thread-safe singletons)

---

### Optimization 4: Bulk Operations

- Batch webhook deliveries to same subscriber endpoint
- Bulk insert delivery attempts for analytics

---

## Security Best Practices

### Practice 1: Secret Management

- Store secrets in `appsettings.json` (development)
- Use Azure Key Vault / AWS Secrets Manager (production)
- Never log secrets (custom log redaction middleware)

---

### Practice 2: Input Validation

- Validate all API inputs with Data Annotations
- Enforce payload size limits (256KB default, 1MB max)
- Sanitize webhook URLs (prevent SSRF: no localhost, 127.0.0.1, 169.254.x.x, etc.)

---

### Practice 3: Dependency Scanning

- Enable NuGet package vulnerability scanning in CI/CD
- Automatic PRs for dependency updates (Dependabot)
- Fail builds on high/critical vulnerabilities

---

## GDPR Compliance Strategy

### Strategy 1: Data Retention Policies

- Configurable retention period per event type (default: 90 days)
- Background job purges expired webhook payloads
- Preserve anonymized metadata (delivery status, timestamp, attempt count)

---

### Strategy 2: Data Export

- API endpoint: `GET /api/subscribers/{id}/data-export`
- Returns JSON with all webhook logs, subscriptions, metadata
- Implement as background job for large datasets (email download link)

---

### Strategy 3: Right to Deletion

- API endpoint: `DELETE /api/subscribers/{id}/data`
- Hard delete personal data (endpoint URLs, custom headers, payloads)
- Preserve anonymized audit trail (delete events logged without PII)

---

## Deployment Architecture

### Container Strategy

- **Three container images**: API, Worker, Dashboard
- **Base image**: `mcr.microsoft.com/dotnet/aspnet:10.0`
- **Multi-stage builds**: Build in SDK image, runtime in ASP.NET image
- **Non-root user**: Run containers as non-root for security

---

### Kubernetes Deployment

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: hookverse-api
spec:
  replicas: 3
  selector:
    matchLabels:
      app: hookverse-api
  template:
    spec:
      containers:
      - name: api
        image: hookverse/api:latest
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "1000m"
        livenessProbe:
          httpGet:
            path: /health
            port: 8080
        readinessProbe:
          httpGet:
            path: /ready
            port: 8080
```

---

### Health Checks

- **Liveness**: `/health` - Returns 200 if process alive
- **Readiness**: `/ready` - Returns 200 if dependencies (database, message bus) reachable
- ASP.NET Core health checks middleware with custom checks

---

## Development Workflow

### Local Development

- **Docker Compose**: Spin up PostgreSQL + RabbitMQ locally
- **appsettings.Development.json**: Local connection strings
- **Hot reload**: .NET 10 supports hot reload for rapid iteration
- **Swagger UI**: Auto-generated API documentation at `/swagger`

---

### CI/CD Pipeline

1. **Build**: `dotnet build --configuration Release`
2. **Test**: `dotnet test --collect:"XPlat Code Coverage"` (80% coverage gate)
3. **Lint**: `dotnet format --verify-no-changes`
4. **Security scan**: NuGet vulnerability check
5. **Docker build**: Multi-stage Dockerfile build
6. **Push**: Docker images to registry
7. **Deploy**: Helm upgrade (Kubernetes)

---

## Conclusion

This research covers all technical decisions required to implement HookVerse according to the specification and constitutional principles. The .NET 10 stack provides high performance, strong typing, and excellent async support. EF Core with provider abstraction gives users flexibility. MassTransit enables pluggable message transports. Blazor Server provides real-time dashboard with code sharing. OpenTelemetry ensures comprehensive observability. All architectural patterns (Clean Architecture, Repository, Outbox) support testability, maintainability, and scalability.

**Next Steps**: Proceed to Phase 1 (data-model.md, contracts/, quickstart.md).
