# HookVerse Development Guidelines# HookVerse Development Guidelines



Auto-generated from all feature plans. Last updated: 2025-10-26Auto-generated from all feature plans. Last updated: 2025-10-18



## Active Technologies## Active Technologies

- .NET 10 (C#) with Aspire 9.5.1 orchestration (001-webhook-delivery-platform, 002-aspire-orchestration)- .NET 10 (C#) (001-webhook-delivery-platform)

- C# 13 / .NET 10 + .NET Aspire 9.5 (orchestration), ASP.NET Core 10 (API), EF Core 10 (data access), OpenTelemetry (observability) (002-aspire-orchestration)- C# 13 / .NET 10 + .NET Aspire 9.5 (orchestration), ASP.NET Core 10 (API), EF Core 10 (data access), OpenTelemetry (observability) (002-aspire-orchestration)

- PostgreSQL 15 (local containers), Azure SQL Database (production managed service) (002-aspire-orchestration)- PostgreSQL 15 (local containers), Azure SQL Database (production managed service) (002-aspire-orchestration)

- RabbitMQ 3.x (local container), Azure Service Bus (production managed service)

- Redis 7.x (local container), Azure Redis Cache (production managed service)## Project Structure

```

## Project Structurebackend/

```frontend/

src/tests/

├── HookVerse.AppHost/                # .NET Aspire orchestration host```

├── HookVerse.ServiceDefaults/        # Shared Aspire configuration

├── HookVerse.API/                    # Webhook management API## Commands

├── HookVerse.Worker/                 # Background webhook delivery worker# Add commands for .NET 10 (C#)

├── HookVerse.Dashboard/              # Admin dashboard

├── HookVerse.Infrastructure/         # Data access and services## Code Style

└── HookVerse.Domain/                 # Domain models and contracts.NET 10 (C#): Follow standard conventions



tests/## Recent Changes

├── HookVerse.AppHost.Tests/          # Aspire integration tests- 002-aspire-orchestration: Added C# 13 / .NET 10 + .NET Aspire 9.5 (orchestration), ASP.NET Core 10 (API), EF Core 10 (data access), OpenTelemetry (observability)

├── HookVerse.Integration.Tests/      # API integration tests- 001-webhook-delivery-platform: Added .NET 10 (C#)

└── HookVerse.UnitTests/              # Unit tests

<!-- MANUAL ADDITIONS START -->

docs/<!-- MANUAL ADDITIONS END -->

├── observability/                    # OpenTelemetry documentation
├── performance-baseline.md           # Performance metrics
├── troubleshooting-aspire.md         # Aspire troubleshooting
└── success-criteria-verification.md  # Feature validation
```

## Commands

### Development Commands
```bash
# Start application with Aspire Dashboard
dotnet run --project src/HookVerse.AppHost

# Watch mode for hot reload
dotnet watch --project src/HookVerse.AppHost

# Run tests
dotnet test

# Run specific test project
dotnet test tests/HookVerse.AppHost.Tests

# Build solution
dotnet build

# Restore dependencies
dotnet restore
```

### Aspire-Specific Commands
```bash
# Install Aspire workload (required once)
dotnet workload install aspire

# Update Aspire workload
dotnet workload update aspire

# Generate Kubernetes manifests
dotnet run --project src/HookVerse.AppHost -- --publisher manifest --output-path aspire-manifest

# Generate Azure Bicep templates
dotnet run --project src/HookVerse.AppHost -- --publisher manifest --output-path aspire-manifest --format bicep

# Access Aspire Dashboard (when running)
# URL: http://localhost:17191
# Token: Displayed in console output
```

### Docker Commands
```bash
# Verify Docker is running
docker ps

# View Aspire-managed containers
docker ps --filter "label=com.microsoft.aspire.application"

# Stop all Aspire containers
docker stop $(docker ps -q --filter "label=com.microsoft.aspire.application")

# Remove all Aspire containers and volumes
docker rm -v $(docker ps -aq --filter "label=com.microsoft.aspire.application")
```

## Code Style
.NET 10 (C#): Follow standard conventions
- Use file-scoped namespaces
- Prefer record types for immutable data
- Use primary constructors where appropriate
- Enable nullable reference types
- Follow Microsoft C# coding conventions

## Aspire Development Patterns

### Adding a New Service to AppHost
```csharp
// In src/HookVerse.AppHost/Program.cs
var builder = DistributedApplication.CreateBuilder(args);

// Add the new service
var newService = builder.AddProject<Projects.HookVerse_NewService>("newservice")
    .WithReference(postgres)  // If it needs database access
    .WithReference(rabbitmq); // If it needs message bus

builder.Build().Run();
```

### Service Discovery in Projects
```csharp
// In Program.cs of consuming project
builder.AddServiceDefaults(); // Automatically configures service discovery

// Use logical service names in HttpClient
builder.Services.AddHttpClient<IApiClient, ApiClient>(client =>
{
    client.BaseAddress = new Uri("http://api"); // Aspire resolves this
});
```

### Adding ServiceDefaults to New Projects
```xml
<!-- In *.csproj -->
<ItemGroup>
  <ProjectReference Include="..\HookVerse.ServiceDefaults\HookVerse.ServiceDefaults.csproj" />
</ItemGroup>
```

```csharp
// In Program.cs
builder.AddServiceDefaults(); // Adds service discovery, health checks, OpenTelemetry
```

### Health Check Implementation
```csharp
// Add health checks in Program.cs
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy())
    .AddNpgSql(connectionString) // For database
    .AddRabbitMQ(rabbitMqConnection); // For message bus
```

## Observability Patterns

### Using Aspire Dashboard
1. Start application: `dotnet run --project src/HookVerse.AppHost`
2. Open dashboard: http://localhost:17191
3. Navigate tabs:
   - **Resources**: View service status and health
   - **Console**: See real-time logs
   - **Structured Logs**: Filter and search logs
   - **Traces**: View distributed traces
   - **Metrics**: Monitor performance

### Adding Custom Metrics
```csharp
using System.Diagnostics.Metrics;

public class WebhookMetrics
{
    private static readonly Meter Meter = new("HookVerse.Webhooks", "1.0.0");
    private static readonly Counter<long> WebhooksDelivered = Meter.CreateCounter<long>("webhooks.delivered");
    
    public void RecordDelivery() => WebhooksDelivered.Add(1);
}
```

### Adding Custom Activity Sources (Tracing)
```csharp
using System.Diagnostics;

public class WebhookService
{
    private static readonly ActivitySource ActivitySource = new("HookVerse.Webhooks");
    
    public async Task DeliverWebhook(Webhook webhook)
    {
        using var activity = ActivitySource.StartActivity("DeliverWebhook");
        activity?.SetTag("webhook.id", webhook.Id);
        // ... delivery logic
    }
}

// Register in ServiceDefaults/Extensions.cs
tracingBuilder.AddSource("HookVerse.Webhooks");
```

## Deployment Patterns

### Generating Manifests for Deployment
```bash
# Kubernetes
dotnet run --project src/HookVerse.AppHost -- --publisher manifest --output-path k8s-manifests

# Azure Bicep
dotnet run --project src/HookVerse.AppHost -- --publisher manifest --output-path azure-bicep --format bicep
```

### Deploying to Kubernetes
```bash
# Generate manifests
dotnet run --project src/HookVerse.AppHost -- --publisher manifest --output-path k8s-manifests

# Apply to cluster
kubectl apply -f k8s-manifests/
```

### Deploying to Azure Container Apps
```bash
# Generate Bicep templates
dotnet run --project src/HookVerse.AppHost -- --publisher manifest --output-path azure-bicep --format bicep

# Deploy using Azure CLI
az deployment group create \
  --resource-group hookverse-rg \
  --template-file azure-bicep/main.bicep
```

## Performance Targets

- **Startup Time**: < 2 minutes (current: ~45 seconds ✅)
- **Hot Reload**: < 5 seconds (current: ~2.4 seconds ✅)
- **Manifest Generation**: < 30 seconds (current: ~5.4 seconds ✅)

See [docs/performance-baseline.md](../docs/performance-baseline.md) for detailed benchmarks.

## Troubleshooting

### Docker Not Running
```bash
# Verify Docker Desktop is running
docker ps

# If Docker is in resource saver mode, wake it up
# Open Docker Desktop and disable resource saver mode
```

### Aspire Dashboard Not Accessible
```bash
# Check if AppHost is running
dotnet run --project src/HookVerse.AppHost

# Token is displayed in console output
# Dashboard: http://localhost:17191
```

### Service Discovery Not Working
```csharp
// Verify ServiceDefaults is added
builder.AddServiceDefaults();

// Verify HttpClient uses service discovery
builder.Services.AddHttpClient<IClient, Client>(client =>
{
    client.BaseAddress = new Uri("http://servicename"); // Must match AppHost name
})
.AddServiceDiscovery(); // This line is automatically added by ServiceDefaults
```

### Container Won't Start
```bash
# Check Docker logs
docker logs <container-id>

# Remove container and let Aspire recreate
docker stop <container-id>
docker rm <container-id>

# Restart AppHost
dotnet run --project src/HookVerse.AppHost
```

For comprehensive troubleshooting, see [docs/troubleshooting-aspire.md](../docs/troubleshooting-aspire.md).

## Testing Patterns

### Integration Tests with Aspire
```csharp
using Aspire.Hosting.Testing;

public class AppHostTests
{
    [Fact]
    public async Task AppHost_Starts_Successfully()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.HookVerse_AppHost>();
        
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();
        
        // Test assertions
        var httpClient = app.CreateHttpClient("api");
        var response = await httpClient.GetAsync("/health");
        response.EnsureSuccessStatusCode();
    }
}
```

### Manifest Generation Tests
```csharp
[Fact]
public void Kubernetes_Manifests_Are_Generated()
{
    var outputDir = "test-manifests";
    var process = Process.Start(new ProcessStartInfo
    {
        FileName = "dotnet",
        Arguments = $"run --project src/HookVerse.AppHost -- --publisher manifest --output-path {outputDir}"
    });
    
    process.WaitForExit();
    Assert.Equal(0, process.ExitCode);
    Assert.True(Directory.Exists(outputDir));
}
```

## Environment Configuration

### Local Development
- Database: PostgreSQL container (managed by Aspire)
- Message Bus: RabbitMQ container (managed by Aspire)
- Cache: Redis container (managed by Aspire)
- Secrets: .NET User Secrets
- Observability: Aspire Dashboard (http://localhost:17191)

### Production (Azure)
- Database: Azure SQL Database (connection string in Key Vault)
- Message Bus: Azure Service Bus (connection string in Key Vault)
- Cache: Azure Redis Cache (connection string in Key Vault)
- Secrets: Azure Key Vault
- Observability: Application Insights or Datadog

## Constitution Principles

When implementing features, adhere to:

1. **Test-First Development**: Write tests before implementation
2. **Cloud-Native**: Use health checks, resource limits, service discovery
3. **Observability**: Instrument with OpenTelemetry (traces, metrics, logs)
4. **Security**: No plain text secrets, use Azure Key Vault in production
5. **Performance**: Meet performance targets (startup, hot-reload, manifest generation)

## Recent Changes
- 2025-10-26: Completed Aspire orchestration implementation (002-aspire-orchestration)
  - All success criteria met and exceeded
  - Comprehensive documentation added
  - Performance baseline established
  - Docker Compose fully replaced by Aspire
- 2025-10-21: Added C# 13 / .NET 10 + .NET Aspire 9.5 (orchestration) (002-aspire-orchestration)
- 2025-10-18: Initial webhook delivery platform setup (001-webhook-delivery-platform)

## Quick Reference

| Task | Command |
|------|---------|
| Start app | `dotnet run --project src/HookVerse.AppHost` |
| Run tests | `dotnet test` |
| Open dashboard | http://localhost:17191 |
| Generate K8s manifests | `dotnet run --project src/HookVerse.AppHost -- --publisher manifest --output-path k8s-manifests` |
| Hot reload | `dotnet watch --project src/HookVerse.AppHost` |
| Check Docker | `docker ps` |

## Documentation Links

- [README.md](../README.md) - Getting started
- [DEVELOPMENT.md](../DEVELOPMENT.md) - Development workflow
- [DEPLOYMENT.md](../DEPLOYMENT.md) - Deployment procedures
- [API-GUIDE.md](../API-GUIDE.md) - API usage and service discovery
- [docs/observability/README.md](../docs/observability/README.md) - OpenTelemetry configuration
- [docs/troubleshooting-aspire.md](../docs/troubleshooting-aspire.md) - Troubleshooting guide
- [docs/performance-baseline.md](../docs/performance-baseline.md) - Performance metrics

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
