# Aspire Troubleshooting Guide

**Last Updated**: October 26, 2025  
**Aspire Version**: 9.5.1

This guide covers common issues when developing HookVerse with .NET Aspire orchestration.

## Table of Contents

1. [Prerequisites & Setup Issues](#prerequisites--setup-issues)
2. [AppHost Issues](#apphost-issues)
3. [Container Issues](#container-issues)
4. [Service Discovery Issues](#service-discovery-issues)
5. [Database Issues](#database-issues)
6. [Message Bus Issues](#message-bus-issues)
7. [Dashboard Issues](#dashboard-issues)
8. [Performance Issues](#performance-issues)
9. [Deployment Issues](#deployment-issues)
10. [Getting Help](#getting-help)

---

## Prerequisites & Setup Issues

### Aspire Workload Not Installed

**Symptom**:
```
error NETSDK1005: Assets file 'project.assets.json' not found.
error: The project depends on the following workload packs that are not installed: aspire
```

**Solution**:
```powershell
# Install Aspire workload
dotnet workload install aspire

# Verify installation
dotnet workload list | findstr aspire

# Expected output:
# aspire    9.5.100/9.0.100    SDK 9.0.100    Installed
```

**If installation fails**:
```powershell
# Update .NET SDK first
dotnet --version  # Should be .NET 10 or higher

# Clear NuGet cache
dotnet nuget locals all --clear

# Try again
dotnet workload install aspire
```

### Docker Desktop Not Running

**Symptom**:
```
Docker.Core.DockerException: Failed to connect to docker daemon
```

**Solution**:
1. **Start Docker Desktop**:
   - Open Docker Desktop application
   - Wait for "Docker is running" notification

2. **Verify Docker is working**:
   ```powershell
   docker ps
   # Should list running containers (may be empty, but shouldn't error)
   ```

3. **Check Docker resources**:
   - Open Docker Desktop → Settings → Resources
   - Ensure minimum: 4 GB RAM, 2 CPUs
   - Recommended: 8 GB RAM, 4 CPUs

### .NET 10 SDK Not Found

**Symptom**:
```
error: The current .NET SDK does not support targeting .NET 10.
```

**Solution**:
```powershell
# Check current SDK version
dotnet --version

# Download .NET 10 SDK from:
# https://dotnet.microsoft.com/download/dotnet/10.0

# After installation, verify:
dotnet --list-sdks
# Should show: 10.x.xxx [C:\Program Files\dotnet\sdk]
```

---

## AppHost Issues

### AppHost Won't Start

**Symptom**: AppHost exits immediately or fails to start

**Common Causes & Solutions**:

**1. Port Already in Use**:
```powershell
# Check if Aspire Dashboard port (17191) is in use
netstat -ano | findstr :17191

# Find the process
Get-Process -Id <PID>

# Kill the process (if safe)
Stop-Process -Id <PID> -Force

# Or change the port in AppHost
```

**Change default port**:
```csharp
// src/HookVerse.AppHost/Program.cs
builder.AddProject<Projects.HookVerse_Api>("api")
    .WithHttpsEndpoint(port: 7001)  // Change from default
```

**2. Missing Dependencies**:
```powershell
# Restore all dependencies
dotnet restore

# Clean and rebuild
dotnet clean
dotnet build
```

**3. Configuration Error**:
```powershell
# Check AppHost logs for details
dotnet run --project src\HookVerse.AppHost --verbosity detailed
```

### AppHost Starts But Services Don't

**Symptom**: AppHost runs, but projects (API, Worker, Dashboard) don't start

**Solutions**:

**1. Check Aspire Dashboard**:
- Open http://localhost:17191
- Go to **Resources** tab
- Look for services with "Failed" or "Stopped" status
- Click on service → View **Console Logs**

**2. Check for compilation errors**:
```powershell
# Build each project individually
dotnet build src\HookVerse.Api
dotnet build src\HookVerse.Worker
dotnet build src\HookVerse.Dashboard
```

**3. Check project references**:
```powershell
# Verify AppHost can see projects
dotnet list src\HookVerse.AppHost package
# Should list project references
```

### "Resource X already exists" Error

**Symptom**:
```
InvalidOperationException: A resource with the name 'postgres' already exists
```

**Solution**:
```csharp
// Check AppHost Program.cs for duplicate resource names
// Each resource name must be unique:

var postgres = builder.AddPostgres("postgres");      // ✓ Unique
var postgres2 = builder.AddPostgres("postgres");     // ✗ Duplicate!

// Fix: Use unique names
var postgres = builder.AddPostgres("postgres");
var postgresTest = builder.AddPostgres("postgres-test");
```

---

## Container Issues

### PostgreSQL Container Won't Start

**Symptom**: PostgreSQL shows "Failed" or "Unhealthy" in Aspire Dashboard

**Solutions**:

**1. Check Docker logs**:
```powershell
# List all containers
docker ps -a | findstr postgres

# View logs
docker logs <container-id>
```

**2. Remove and restart**:
```powershell
# Stop AppHost (Ctrl+C)

# Remove PostgreSQL containers
docker ps -a | findstr postgres | ForEach-Object { docker rm -f $_.Split()[0] }

# Restart AppHost
dotnet run --project src\HookVerse.AppHost
```

**3. Check for data volume conflicts**:
```powershell
# Remove volumes (⚠️ This deletes data!)
docker volume ls | findstr hookverse
docker volume rm <volume-name>
```

**4. Port conflict (5432)**:
```powershell
# Check if port 5432 is in use
netstat -ano | findstr :5432

# Option 1: Stop conflicting service
# Option 2: Change PostgreSQL port in AppHost:
```

```csharp
// src/HookVerse.AppHost/Program.cs
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin()
    .WithHostPort(5433);  // Use different port
```

### RabbitMQ Container Fails

**Symptom**: RabbitMQ container exits or is unhealthy

**Solutions**:

**1. Check RabbitMQ logs**:
```powershell
docker logs <rabbitmq-container-id>
```

**2. Common errors**:

**Error: "Failed to create dir: /var/lib/rabbitmq/mnesia"**
- **Solution**: Remove volume and restart
  ```powershell
  docker volume rm <rabbitmq-volume>
  ```

**Error: "Port 5672 already in use"**
- **Solution**: Change port in AppHost
  ```csharp
  var rabbitmq = builder.AddRabbitMQ("rabbitmq")
      .WithHostPort(5673);  // Different port
  ```

**3. Reset RabbitMQ completely**:
```powershell
# Stop AppHost
# Remove all RabbitMQ containers and volumes
docker ps -a | findstr rabbitmq | ForEach-Object { docker rm -f $_.Split()[0] }
docker volume ls | findstr rabbitmq | ForEach-Object { docker volume rm $_.Split()[1] }
```

### Redis Container Issues

**Symptom**: Redis container fails to start or is unhealthy

**Solutions**:

**1. Check Redis logs**:
```powershell
docker logs <redis-container-id>
```

**2. Port conflict (6379)**:
```powershell
# Check port usage
netstat -ano | findstr :6379

# Change Redis port:
```
```csharp
var redis = builder.AddRedis("redis")
    .WithHostPort(6380);
```

**3. Memory issues**:
```powershell
# Redis needs sufficient memory
# Check Docker Desktop → Settings → Resources
# Increase Memory to at least 4 GB
```

### All Containers Fail to Start

**Symptom**: All containers show errors

**Solutions**:

**1. Check Docker Desktop status**:
- Open Docker Desktop
- Ensure it says "Engine running"
- Check for updates

**2. Reset Docker**:
- Docker Desktop → Troubleshoot → Reset to factory defaults
- ⚠️ This removes all containers and images

**3. Check disk space**:
```powershell
# Docker needs disk space for images and containers
docker system df

# Clean up if needed:
docker system prune -a --volumes
```

---

## Service Discovery Issues

### Service Can't Connect to Database

**Symptom**:
```
Npgsql.NpgsqlException: Failed to connect to server 'postgres:5432'
```

**Solutions**:

**1. Verify AppHost is running**:
- Service discovery only works when AppHost is running
- Check Aspire Dashboard shows all resources as "Running"

**2. Check connection string**:
```powershell
# In Aspire Dashboard:
# Resources → API → Environment tab
# Look for ConnectionStrings__postgres

# Should be: Server=localhost;Port=xxxxx;Database=hookverse;...
```

**3. Check ServiceDefaults reference**:
```xml
<!-- All services must reference ServiceDefaults -->
<ProjectReference Include="..\HookVerse.ServiceDefaults\HookVerse.ServiceDefaults.csproj" />
```

**4. Verify AddServiceDefaults() is called**:
```csharp
// In Program.cs of API, Worker, Dashboard
var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();  // ← Must be present
```

### "No such host is known" Error

**Symptom**:
```
System.Net.Sockets.SocketException: No such host is known (api)
System.Net.Sockets.SocketException: No such host is known (postgres)
```

**Solutions**:

**1. Service name mismatch**:
```csharp
// AppHost definition:
var api = builder.AddProject<Projects.HookVerse_Api>("api");

// Consumer must use exact same name:
builder.Services.AddHttpClient("api", client => {
    client.BaseAddress = new Uri("http://api");  // ← Must match AppHost name
});
```

**2. Running service outside AppHost**:
```powershell
# Don't do this when using Aspire:
dotnet run --project src\HookVerse.Api  # ✗ Won't have service discovery

# Instead, run through AppHost:
dotnet run --project src\HookVerse.AppHost  # ✓ Correct
```

**3. Missing service discovery configuration**:
```csharp
// ServiceDefaults/Extensions.cs should have:
builder.Services.AddServiceDiscovery();
builder.Services.ConfigureHttpClientDefaults(http => {
    http.AddServiceDiscovery();
});
```

### Circular Service Discovery

**Symptom**: Services fail to start with dependency timeout

**Solution**: Avoid HTTP calls between services during startup. Use message bus instead:

```csharp
// ✗ Bad: HTTP call during startup
public class Startup {
    public void Configure() {
        var data = _httpClient.GetAsync("http://api/data").Result;  // Blocks!
    }
}

// ✓ Good: HTTP calls only after startup
public class MyService {
    public async Task<Data> GetDataAsync() {
        return await _httpClient.GetFromJsonAsync<Data>("http://api/data");
    }
}
```

---

## Database Issues

### Migration Fails

**Symptom**:
```
dotnet ef database update
Build FAILED.
```

**Solutions**:

**1. Ensure AppHost is running**:
```powershell
# Terminal 1:
dotnet run --project src\HookVerse.AppHost

# Terminal 2 (after all services are Running):
cd src\HookVerse.Infrastructure
dotnet ef database update --startup-project ..\HookVerse.Api
```

**2. Check PostgreSQL is healthy**:
- Open Aspire Dashboard
- Resources → postgres should be "Running"
- Click on postgres → Console Logs (check for errors)

**3. Connection string issues**:
```powershell
# Test connection manually
docker ps | findstr postgres
docker exec -it <container-id> psql -U hookverse -d hookverse

# If this works, EF connection string might be wrong
```

**4. Migration files corrupted**:
```powershell
# Delete and recreate migration
dotnet ef migrations remove --project src\HookVerse.Infrastructure --startup-project src\HookVerse.Api
dotnet ef migrations add InitialCreate --project src\HookVerse.Infrastructure --startup-project src\HookVerse.Api
dotnet ef database update --project src\HookVerse.Infrastructure --startup-project src\HookVerse.Api
```

### Database Doesn't Persist

**Symptom**: Data is lost when AppHost restarts

**Explanation**: By default, Aspire uses ephemeral containers for local development.

**Solution for persistent data**:
```csharp
// src/HookVerse.AppHost/Program.cs
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()  // ← Persists data across restarts
    .WithPgAdmin();
```

**Or use a persistent volume name**:
```csharp
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("hookverse-postgres-data")  // Named volume
    .WithPgAdmin();
```

### "Database does not exist" Error

**Symptom**:
```
Npgsql.PostgresException: database "hookverse" does not exist
```

**Solutions**:

**1. Database wasn't created**:
```csharp
// In AppHost, ensure WithDatabase() is called:
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin();

var hookverseDb = postgres.AddDatabase("hookverse");  // ← Creates database

// Then reference it in services:
builder.AddProject<Projects.HookVerse_Api>("api")
    .WithReference(hookverseDb);
```

**2. Manual database creation**:
```powershell
# Connect to PostgreSQL container
docker exec -it <postgres-container> psql -U postgres

# Create database
CREATE DATABASE hookverse;
GRANT ALL PRIVILEGES ON DATABASE hookverse TO hookverse;
```

---

## Message Bus Issues

### Worker Not Consuming Messages

**Symptom**: Webhooks created but no deliveries happening

**Solutions**:

**1. Check Worker logs**:
```powershell
# In Aspire Dashboard:
# Console Logs → Select "hookverse-worker"
# Look for: "Connected to RabbitMQ" or "Listening for messages"
```

**2. Check RabbitMQ Management UI**:
- Open http://localhost:15672
- Login: guest/guest
- **Connections** tab: Verify Worker is connected
- **Queues** tab: Check `webhook-deliveries` queue has consumers

**3. Queue not created**:
```csharp
// Worker should declare queue on startup
// Check MessageBusExtensions.cs:
channel.QueueDeclare(
    queue: "webhook-deliveries",
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: null);
```

**4. Messages stuck in queue**:
```powershell
# In RabbitMQ Management UI:
# Queues → webhook-deliveries
# If "Ready" count > 0 but not decreasing:

# Option 1: Restart Worker
# Option 2: Purge queue (deletes messages!)
# Option 3: Check Worker logs for consumption errors
```

### RabbitMQ Connection Refused

**Symptom**:
```
RabbitMQ.Client.Exceptions.BrokerUnreachableException: None of the specified endpoints were reachable
```

**Solutions**:

**1. Check RabbitMQ is running**:
```powershell
# Aspire Dashboard → Resources → rabbitmq → should be "Running"
docker ps | findstr rabbitmq
```

**2. Check connection string**:
```powershell
# In Aspire Dashboard:
# Resources → Worker → Environment
# Look for: ConnectionStrings__rabbitmq
# Should be: amqp://guest:guest@localhost:xxxxx
```

**3. Port mismatch**:
```csharp
// AppHost defines the port:
var rabbitmq = builder.AddRabbitMQ("rabbitmq");

// Worker gets connection string automatically via service discovery
// Don't hardcode connection strings!
```

### Message Serialization Errors

**Symptom**:
```
System.Text.Json.JsonException: The JSON value could not be converted
```

**Solutions**:

**1. Ensure consistent DTOs**:
```csharp
// Publisher (API):
var message = new WebhookDeliveryMessage {
    WebhookId = webhookId,
    EventTypeId = eventTypeId
};

// Consumer (Worker):
var message = JsonSerializer.Deserialize<WebhookDeliveryMessage>(body);
// DTO must match exactly!
```

**2. Check JSON serialization options**:
```csharp
// Use same JsonSerializerOptions in both publisher and consumer
var options = new JsonSerializerOptions {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};
```

---

## Dashboard Issues

### Can't Access Aspire Dashboard

**Symptom**: http://localhost:17191 doesn't load

**Solutions**:

**1. Check if AppHost is running**:
```powershell
# Dashboard only works when AppHost is running
Get-Process | findstr HookVerse.AppHost
```

**2. Check port is correct**:
```powershell
# Look for dashboard URL in AppHost output:
# info: Aspire.Hosting.Dashboard.DashboardService[0]
#       Now listening on: http://localhost:17191
```

**3. Port conflict**:
```powershell
netstat -ano | findstr :17191

# If in use, AppHost will choose different port
# Check AppHost console output for actual port
```

**4. Browser issues**:
- Try different browser
- Clear browser cache
- Try incognito/private mode

### Dashboard Token Expired

**Symptom**: "Token expired" or authentication error

**Solution**:
```powershell
# Restart AppHost to get new token
# Ctrl+C to stop
dotnet run --project src\HookVerse.AppHost

# Look for output:
# Login to the dashboard at http://localhost:17191/login?t=<token>
# Copy the full URL with token
```

### Dashboard Shows No Resources

**Symptom**: Resources tab is empty

**Solutions**:

**1. AppHost hasn't finished starting**:
- Wait a few seconds
- Refresh browser

**2. No resources defined**:
```csharp
// Check AppHost Program.cs has resources:
var postgres = builder.AddPostgres("postgres");  // Should appear in Resources
var api = builder.AddProject<Projects.HookVerse_Api>("api");  // Should appear
```

**3. AppHost crashed**:
- Check AppHost console for errors
- Look for red error messages

### Logs Not Appearing

**Symptom**: Console Logs or Structured Logs tabs are empty

**Solutions**:

**1. Service isn't running**:
- Check Resources tab - service must be "Running"

**2. No logs being generated**:
- Make API requests to generate logs
- Check service startup code has logging

**3. Filters active**:
- Click "Clear filters" button
- Check log level filter (set to "Trace" to see all)

---

## Performance Issues

### Slow Startup Time

**Symptom**: AppHost takes > 2 minutes to start all services

**Solutions**:

**1. Disable unnecessary services**:
```csharp
// Comment out services you don't need:
// var dashboard = builder.AddProject<Projects.HookVerse_Dashboard>("dashboard");
```

**2. Reduce parallel startup**:
```csharp
// Let containers start before services:
var postgres = builder.AddPostgres("postgres").WithPgAdmin();
var rabbitmq = builder.AddRabbitMQ("rabbitmq").WithManagementUI();
var redis = builder.AddRedis("redis");

// Wait a bit before adding services (if needed)
```

**3. Check Docker performance**:
- Docker Desktop → Settings → Resources
- Increase CPU and Memory allocation

**4. Use local NuGet packages**:
```powershell
# Clear and rebuild to avoid network fetches
dotnet nuget locals all --clear
dotnet restore --force
```

### High Memory Usage

**Symptom**: System slows down, AppHost uses > 8 GB RAM

**Solutions**:

**1. Check container limits**:
```csharp
// Limit container memory:
var postgres = builder.AddPostgres("postgres")
    .WithAnnotation("container.memory", "512m");
```

**2. Stop unused containers**:
```powershell
docker ps
docker stop <container-id>
```

**3. Increase Docker resources**:
- Docker Desktop → Settings → Resources
- Increase Memory Limit

### Hot Reload Takes > 5 Seconds

**Symptom**: Code changes take too long to reflect

**Solutions**:

**1. Reduce project size**:
- Smaller projects rebuild faster
- Consider splitting large services

**2. Disable certain services during development**:
```csharp
// Temporarily disable non-essential services
if (builder.Environment.IsDevelopment()) {
    // Only start what you need
}
```

**3. Use `dotnet watch` directly** (without Aspire):
```powershell
# For single-service development:
cd src\HookVerse.Api
dotnet watch run
```

---

## Deployment Issues

### Manifest Generation Fails

**Symptom**:
```
dotnet publish --output ./manifests
ERROR: No container registry specified
```

**Solutions**:

**1. Specify container registry**:
```powershell
# For Azure Container Registry:
dotnet publish -p:PublishProfile=DefaultContainer -p:ContainerRegistry=myregistry.azurecr.io

# For Docker Hub:
dotnet publish -p:PublishProfile=DefaultContainer -p:ContainerRegistry=docker.io/username
```

**2. Use manifest generation script**:
```powershell
# See DEPLOYMENT.md for full script
.\scripts\generate-manifests.ps1 -Environment Production -Registry myregistry.azurecr.io
```

**3. Check publisher configuration**:
```csharp
// AppHost should have publisher configured:
// This is automatic with Aspire 9.5
```

### Kubernetes Deployment Fails

**Symptom**: `kubectl apply` fails with validation errors

**Solutions**:

**1. Validate manifests first**:
```powershell
kubectl apply --dry-run=client -f ./manifests/kubernetes/
```

**2. Check resource names**:
```yaml
# Kubernetes names must be lowercase and alphanumeric
# Aspire may generate invalid names

# Fix in manifests:
name: hookverse-api  # ✓ Valid
name: HookVerse.Api  # ✗ Invalid
```

**3. Add required labels**:
```yaml
# Kubernetes manifests need proper labels
metadata:
  labels:
    app: hookverse-api
```

### Azure Container Apps Deployment Fails

**Symptom**: `az containerapp create` fails

**Solutions**:

**1. Check Azure CLI is logged in**:
```powershell
az login
az account show
```

**2. Verify resource group exists**:
```powershell
az group create --name hookverse-rg --location eastus
```

**3. Check container registry access**:
```powershell
az acr login --name myregistry
```

**4. Verify Bicep template is valid**:
```powershell
az deployment group validate --resource-group hookverse-rg --template-file ./manifests/azure/main.bicep
```

---

## Getting Help

### Diagnostic Information to Collect

When reporting issues, include:

**1. Environment Info**:
```powershell
dotnet --info
dotnet workload list
docker --version
```

**2. AppHost Output**:
```powershell
dotnet run --project src\HookVerse.AppHost --verbosity detailed > apphost.log 2>&1
```

**3. Container Status**:
```powershell
docker ps -a
docker logs <container-id>
```

**4. Aspire Dashboard Info**:
- Screenshot of Resources tab
- Console Logs for failing service
- Structured Logs with errors

### Resources

- **Official Docs**: https://learn.microsoft.com/dotnet/aspire/
- **GitHub Issues**: https://github.com/evilz/HookVerse/issues
- **Aspire GitHub**: https://github.com/dotnet/aspire
- **Stack Overflow**: Tag `dotnet-aspire`

### Common Error Codes

| Error Code | Description | Quick Fix |
|------------|-------------|-----------|
| NETSDK1005 | Aspire workload missing | `dotnet workload install aspire` |
| NETSDK1057 | Preview SDK warning | Informational, can ignore |
| Docker.Core.DockerException | Docker not running | Start Docker Desktop |
| Npgsql.NpgsqlException | Database connection failed | Check AppHost is running |
| BrokerUnreachableException | RabbitMQ connection failed | Check RabbitMQ container status |

### Debug Mode

Enable verbose logging for troubleshooting:

```json
// appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Debug",
      "Microsoft.Hosting.Lifetime": "Information",
      "Aspire": "Debug"
    }
  }
}
```

```powershell
# Run with detailed output
dotnet run --project src\HookVerse.AppHost -- --verbosity detailed
```

### Reset Everything

If all else fails, complete reset:

```powershell
# 1. Stop all processes
# Press Ctrl+C in AppHost terminal

# 2. Remove all containers
docker ps -a | findstr hookverse | ForEach-Object { docker rm -f $_.Split()[0] }

# 3. Remove all volumes
docker volume ls | findstr hookverse | ForEach-Object { docker volume rm $_.Split()[1] }

# 4. Clean solution
dotnet clean
Remove-Item -Recurse -Force bin,obj -ErrorAction SilentlyContinue

# 5. Restore and rebuild
dotnet restore
dotnet build

# 6. Restart AppHost
dotnet run --project src\HookVerse.AppHost
```

---

**Last Updated**: October 26, 2025  
**Maintainers**: HookVerse Team  
**Feedback**: Open an issue or submit a PR
