# Quickstart: .NET Aspire Orchestration for HookVerse

**Feature**: 002-aspire-orchestration  
**Date**: 2025-10-21  
**Target Audience**: Developers setting up local development or deploying to Azure/Kubernetes

---

## Prerequisites

### Required Software

- **.NET 10 SDK** (or later) with Aspire workload:
  ```powershell
  dotnet workload install aspire
  ```

- **Docker Desktop** or **Podman** (for local container dependencies):
  - Windows: [Docker Desktop](https://www.docker.com/products/docker-desktop/)
  - Linux: Docker Engine or Podman
  - macOS: Docker Desktop

- **IDE** (one of):
  - Visual Studio 2025 (with .NET Aspire support)
  - VS Code with C# Dev Kit
  - Rider 2025.x

### Optional (for deployment)

- **Kubernetes**: kubectl + cluster access (for Kubernetes deployment)
- **Azure CLI**: `az` CLI logged in (for Azure deployment)

### Verification

```powershell
# Verify .NET 10 SDK
dotnet --version
# Expected: 10.0.x or higher

# Verify Aspire workload
dotnet workload list
# Expected: "aspire" in the list

# Verify Docker
docker --version
# Expected: Docker version 24.x or higher (or Podman)
```

---

## Scenario 1: Local Development (First-Time Setup)

**Goal**: Start all HookVerse services + dependencies locally in <2 minutes.

### Step 1: Clone and Navigate

```powershell
git clone https://github.com/evilz/HookVerse.git
cd HookVerse
git checkout 002-aspire-orchestration
```

### Step 2: Start Aspire AppHost

```powershell
# Run the Aspire orchestrator
dotnet run --project src/HookVerse.AppHost

# Expected output:
# Building...
# info: Aspire.Hosting.DistributedApplication[0]
#       Aspire version: 9.5.0
# info: Aspire.Hosting.DistributedApplication[0]
#       Distributed application started. Press Ctrl+C to shut down.
# info: Aspire.Hosting.DistributedApplication[0]
#       Dashboard: http://localhost:15888
```

**What happens**:
1. Aspire pulls and starts Docker containers:
   - PostgreSQL (port 5432) + PgAdmin (port 5050)
   - RabbitMQ (port 5672) + Management UI (port 15672)
   - Redis (port 6379)
2. Aspire starts .NET services:
   - HookVerse.Api (https://localhost:7001)
   - HookVerse.Worker (background process)
   - HookVerse.Dashboard (https://localhost:7002)
3. Aspire starts observability dashboard (http://localhost:15888)

### Step 3: Open Aspire Dashboard

Navigate to **http://localhost:15888** in your browser.

**Dashboard features**:
- **Resources**: View status of all services and containers (running, stopped, health)
- **Logs**: Unified logs from all services (filterable, searchable)
- **Traces**: Distributed tracing (view request flows across services)
- **Metrics**: CPU, memory, request rates, latency (p50, p95, p99)

### Step 4: Verify Services

```powershell
# Check API health
curl https://localhost:7001/health/ready
# Expected: {"status":"Healthy"}

# Check Worker is processing (view logs in Aspire dashboard)
# Navigate to Dashboard > Logs > Filter by "worker"
# Expected: Logs showing "Worker processing queue..."

# Check Dashboard is accessible
curl https://localhost:7002
# Expected: HTML response (dashboard homepage)
```

### Step 5: Access Management UIs (Optional)

- **PgAdmin** (PostgreSQL UI): http://localhost:5050
  - Email: `admin@hookverse.local`
  - Password: (auto-generated, visible in Aspire dashboard logs)

- **RabbitMQ Management**: http://localhost:15672
  - Username: `guest`
  - Password: `guest`

### Step 6: Make Code Changes (Hot Reload)

1. Open `src/HookVerse.Api/Controllers/WebhooksController.cs` in your IDE
2. Make a change (e.g., add a comment or log statement)
3. Save the file

**Expected**: API automatically reloads within <5 seconds (no manual restart). Check Aspire dashboard logs for "Hot reload applied" message.

### Step 7: Stop Everything

Press **Ctrl+C** in the terminal where `dotnet run` is running.

**What happens**:
- All .NET services gracefully shut down
- All Docker containers stop
- Aspire dashboard closes

---

## Scenario 2: Debugging a Specific Service

**Goal**: Debug the API service while keeping other services running.

### Step 1: Start Aspire (if not already running)

```powershell
dotnet run --project src/HookVerse.AppHost
```

### Step 2: Attach Debugger (Visual Studio)

1. Open `HookVerse.sln` in Visual Studio
2. Set breakpoint in `HookVerse.Api/Controllers/WebhooksController.cs`
3. Go to **Debug > Attach to Process**
4. Filter by "HookVerse.Api"
5. Attach

### Step 3: Attach Debugger (VS Code)

1. Open workspace in VS Code
2. Set breakpoint in `HookVerse.Api/Controllers/WebhooksController.cs`
3. Open Command Palette (Ctrl+Shift+P)
4. Run **.NET: Attach to Process**
5. Select `HookVerse.Api` process

### Step 4: Trigger Breakpoint

```powershell
# Send API request
curl -X POST https://localhost:7001/api/webhooks \
  -H "Content-Type: application/json" \
  -d '{"url":"https://example.com","events":["order.created"]}'
```

**Expected**: Debugger breaks at your breakpoint. Inspect variables, step through code, etc.

---

## Scenario 3: Generate Kubernetes Manifests

**Goal**: Generate deployment YAML for Kubernetes.

### Step 1: Generate Manifests

```powershell
# Navigate to repo root
cd HookVerse

# Generate Kubernetes manifests
dotnet publish src/HookVerse.AppHost/HookVerse.AppHost.csproj `
  --output aspire/manifests/kubernetes `
  /p:PublishProfile=kubernetes

# Expected output:
# Microsoft (R) Build Engine version 18.0.0+...
# Build succeeded.
# Manifest generation succeeded.
# Output: aspire/manifests/kubernetes/
```

### Step 2: Review Generated Files

```powershell
ls aspire/manifests/kubernetes/

# Expected files:
# - deployment.yaml (Deployments for api, worker, dashboard)
# - service.yaml (Services for network exposure)
# - configmap.yaml (Configuration)
```

### Step 3: Inspect Manifests

```powershell
# View API deployment
cat aspire/manifests/kubernetes/deployment.yaml | Select-String -Pattern "kind: Deployment" -Context 20,50

# Verify resource limits are present
cat aspire/manifests/kubernetes/deployment.yaml | Select-String -Pattern "resources:"

# Expected:
# resources:
#   limits:
#     cpu: "500m"
#     memory: "512Mi"
```

### Step 4: Validate Manifests

```powershell
# Validate against Kubernetes schema (requires kubectl)
kubectl apply --dry-run=client -f aspire/manifests/kubernetes/

# Expected: No errors (if any, manifests are invalid)
```

### Step 5: Deploy to Kubernetes (Optional)

```powershell
# Ensure kubectl is connected to your cluster
kubectl config current-context

# Apply manifests
kubectl apply -f aspire/manifests/kubernetes/

# Watch deployment progress
kubectl get pods -w

# Expected: Pods start and reach "Running" status
```

---

## Scenario 4: Generate Azure Bicep Templates

**Goal**: Generate Infrastructure-as-Code for Azure deployment.

### Step 1: Generate Bicep

```powershell
# Navigate to repo root
cd HookVerse

# Generate Azure Bicep templates
dotnet publish src/HookVerse.AppHost/HookVerse.AppHost.csproj `
  --output aspire/manifests/azure `
  /p:PublishProfile=azure

# Expected output:
# Build succeeded.
# Bicep generation succeeded.
# Output: aspire/manifests/azure/
```

### Step 2: Review Generated Files

```powershell
ls aspire/manifests/azure/

# Expected files:
# - main.bicep (Azure resources: Container Apps, SQL, Service Bus, Redis, Key Vault)
# - parameters.json (Environment-specific values)
```

### Step 3: Inspect Bicep

```powershell
# View Azure resources
cat aspire/manifests/azure/main.bicep | Select-String -Pattern "resource" | Select-Object -First 10

# Expected resources:
# - Microsoft.KeyVault/vaults (Key Vault)
# - Microsoft.Sql/servers (Azure SQL)
# - Microsoft.ServiceBus/namespaces (Service Bus)
# - Microsoft.Cache/redis (Redis Cache)
# - Microsoft.App/containerApps (Container Apps)
```

### Step 4: Customize Parameters

Edit `aspire/manifests/azure/parameters.json`:

```json
{
  "environmentName": "prod",
  "location": "eastus",
  "sqlAdminPassword": "<your-secure-password>",
  "apiReplicas": 3
}
```

### Step 5: Validate Bicep

```powershell
# Validate syntax (requires Azure CLI)
az bicep build --file aspire/manifests/azure/main.bicep

# Expected: No errors (generates ARM template)
```

### Step 6: Deploy to Azure (Optional)

```powershell
# Ensure Azure CLI is logged in
az login
az account set --subscription <your-subscription-id>

# Create resource group
az group create --name hookverse-prod --location eastus

# Deploy Bicep
az deployment group create `
  --resource-group hookverse-prod `
  --template-file aspire/manifests/azure/main.bicep `
  --parameters @aspire/manifests/azure/parameters.json

# Expected: Deployment succeeds (5-10 minutes)
```

### Step 7: Get API URL

```powershell
# Query deployment outputs
az deployment group show `
  --resource-group hookverse-prod `
  --name main `
  --query properties.outputs.apiUrl.value

# Expected: https://ca-hookverse-api-prod.azurecontainerapps.io
```

---

## Scenario 5: Add a New Service

**Goal**: Add a new .NET project to the Aspire orchestration.

### Step 1: Create New Project

```powershell
# Navigate to src/
cd src

# Create new project (e.g., notification service)
dotnet new webapi -n HookVerse.Notifications

# Add project to solution
dotnet sln ../HookVerse.sln add HookVerse.Notifications/HookVerse.Notifications.csproj
```

### Step 2: Add ServiceDefaults Reference

```powershell
cd HookVerse.Notifications

# Add ServiceDefaults package reference
dotnet add reference ../HookVerse.ServiceDefaults/HookVerse.ServiceDefaults.csproj
```

### Step 3: Integrate ServiceDefaults

Edit `HookVerse.Notifications/Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add service defaults (service discovery, health checks, telemetry)
builder.AddServiceDefaults();

// Add health checks for dependencies
builder.Services.AddHealthChecks()
    .AddDefaultHealthChecks(builder.Configuration);

// ... rest of service configuration ...

var app = builder.Build();

// Map health check endpoints
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();
```

### Step 4: Register in AppHost

Edit `src/HookVerse.AppHost/Program.cs`:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Existing resources...
var postgres = builder.AddPostgres("postgres").WithPgAdmin();
var rabbitmq = builder.AddRabbitMQ("rabbitmq").WithManagementPlugin();
var redis = builder.AddRedis("redis");

// ADD NEW SERVICE
var notifications = builder.AddProject<Projects.HookVerse_Notifications>("notifications")
    .WithReference(rabbitmq) // Notifications consume RabbitMQ events
    .WithReference(redis)    // Notifications use Redis for caching
    .WithReplicas(2)
    .WithCpuLimit("250m")
    .WithMemoryLimit("256Mi");

// Existing services...
var api = builder.AddProject<Projects.HookVerse_Api>("api")
    .WithReference(postgres)
    .WithReference(rabbitmq)
    .WithReference(redis)
    .WithReference(notifications); // API calls Notifications service

builder.Build().Run();
```

### Step 5: Verify New Service

```powershell
# Start Aspire
dotnet run --project src/HookVerse.AppHost

# Check Aspire dashboard (http://localhost:15888)
# Expected: "notifications" service appears in Resources list

# Verify health
curl https://localhost:7003/health/ready  # Port auto-assigned by Aspire
# Expected: {"status":"Healthy"}
```

### Step 6: Call New Service from API

In `HookVerse.Api`, inject HTTP client:

```csharp
// Startup configuration
builder.Services.AddHttpClient<INotificationClient, NotificationClient>(
    client => client.BaseAddress = new Uri("http://notifications")); // Service discovery!

// Usage in controller
public class WebhooksController : ControllerBase
{
    private readonly INotificationClient _notificationClient;

    public WebhooksController(INotificationClient notificationClient)
    {
        _notificationClient = notificationClient;
    }

    [HttpPost]
    public async Task<IActionResult> CreateWebhook(WebhookRequest request)
    {
        // Business logic...

        // Call notifications service
        await _notificationClient.SendAsync(new Notification
        {
            Type = "webhook.created",
            Recipient = request.Email
        });

        return Ok();
    }
}
```

**No connection strings to configure!** Aspire resolves `http://notifications` automatically.

---

## Scenario 6: Troubleshooting

### Issue: "Aspire dashboard won't start"

**Symptom**: `dotnet run --project src/HookVerse.AppHost` fails with port conflict.

**Solution**:
```powershell
# Check if port 15888 is in use
netstat -ano | findstr 15888

# If in use, kill the process or change Aspire dashboard port
# Edit launchSettings.json:
# "applicationUrl": "http://localhost:16000"  # Use different port
```

### Issue: "Docker containers won't start"

**Symptom**: Aspire dashboard shows containers in "Stopped" state.

**Solution**:
```powershell
# Verify Docker is running
docker ps

# If Docker Desktop is stopped, start it
# For Podman users, ensure Podman socket is running:
podman system service --time=0
```

### Issue: "Service can't connect to PostgreSQL"

**Symptom**: API logs show "Npgsql connection failed".

**Solution**:
```powershell
# Check PostgreSQL container is running
docker ps | findstr postgres

# Check connection string in Aspire dashboard
# Navigate to Dashboard > Resources > postgres > Environment
# Verify ConnectionStrings:postgres is set

# Manually test connection
docker exec -it <postgres-container-id> psql -U hookverse -d hookverse
```

### Issue: "Hot reload not working"

**Symptom**: Code changes don't reflect in running service.

**Solution**:
```powershell
# Ensure .NET 10 hot reload is enabled
dotnet watch --project src/HookVerse.Api

# OR restart Aspire AppHost
# Press Ctrl+C, then:
dotnet run --project src/HookVerse.AppHost
```

### Issue: "Generated manifests are missing resources"

**Symptom**: `kubectl apply` fails with "resource not found".

**Solution**:
```powershell
# Ensure all services are registered in AppHost/Program.cs
# Each service must call .AddProject<T>()

# Regenerate manifests
dotnet publish src/HookVerse.AppHost `
  --output aspire/manifests/kubernetes `
  /p:PublishProfile=kubernetes `
  --force  # Force regeneration

# Verify all resources present
kubectl apply --dry-run=client -f aspire/manifests/kubernetes/
```

---

## Scenario 7: CI/CD Integration

**Goal**: Integrate Aspire manifest generation into GitHub Actions.

### Step 1: Update Workflow

Edit `.github/workflows/deploy.yml`:

```yaml
name: Deploy to Kubernetes

on:
  push:
    branches: [main]

jobs:
  deploy:
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v4

    - name: Setup .NET 10
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '10.0.x'

    - name: Install Aspire workload
      run: dotnet workload install aspire

    - name: Generate Kubernetes manifests
      run: |
        dotnet publish src/HookVerse.AppHost/HookVerse.AppHost.csproj \
          --output aspire/manifests/kubernetes \
          /p:PublishProfile=kubernetes

    - name: Validate manifests
      run: |
        kubectl apply --dry-run=client -f aspire/manifests/kubernetes/

    - name: Deploy to Kubernetes
      run: |
        kubectl apply -f aspire/manifests/kubernetes/
      env:
        KUBECONFIG: ${{ secrets.KUBECONFIG }}

    - name: Wait for rollout
      run: |
        kubectl rollout status deployment/api
        kubectl rollout status deployment/worker
```

### Step 2: Test Workflow

```powershell
# Commit changes
git add .github/workflows/deploy.yml
git commit -m "ci: integrate Aspire manifest generation"
git push

# Monitor workflow in GitHub Actions UI
# Expected: Workflow succeeds, manifests generated and deployed
```

---

## Next Steps

1. **Read Data Model**: Review [data-model.md](./data-model.md) for configuration entities
2. **Review Contracts**: See [contracts/aspire-contracts.md](./contracts/aspire-contracts.md) for detailed schemas
3. **Write Tests**: Follow test-first development (see Constitution Principle V)
4. **Customize Resources**: Adjust CPU/memory limits in AppHost/Program.cs as needed
5. **Configure Observability**: Set `OTEL_EXPORTER_OTLP_ENDPOINT` for production telemetry

---

## Success Criteria Verification

| Criterion | How to Verify | Expected Result |
|-----------|---------------|-----------------|
| SC-001: 2-minute startup | `Measure-Command { dotnet run --project src/HookVerse.AppHost }` | <120 seconds |
| SC-002: Zero manual config | Count connection strings in appsettings.json | 0 hardcoded strings |
| SC-003: Hot reload <5sec | Measure time from save to "Hot reload applied" log | <5 seconds |
| SC-004: 30sec manifest gen | `Measure-Command { dotnet publish ... }` | <30 seconds |
| SC-005: Health checks work | `curl https://localhost:7001/health/ready` | 200 OK |
| SC-006: Telemetry visible | Open Aspire dashboard, check Traces tab | Traces appear |

Run these checks after completing setup to validate success criteria from spec.md.
