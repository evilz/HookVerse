# HookVerse - Running and Debugging Guide

**Last Updated**: October 26, 2025  
**Version**: 2.0.0 (Aspire Edition)

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Quick Start](#quick-start)
3. [Using the Aspire Dashboard](#using-the-aspire-dashboard)
4. [Debugging in Visual Studio Code](#debugging-in-visual-studio-code)
5. [Debugging in Visual Studio](#debugging-in-visual-studio)
6. [Using the API](#using-the-api)
7. [Using the Dashboard](#using-the-dashboard)
8. [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Required Software

- **.NET 10 SDK** (preview) - [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
- **.NET Aspire workload** - Install with: `dotnet workload install aspire`
- **Docker Desktop** - [Download](https://www.docker.com/products/docker-desktop)

### Optional Tools

- **Visual Studio Code** with C# Dev Kit extension
- **Visual Studio 2022** (17.8+) with Aspire support
- **Postman** or **Thunder Client** for API testing

### Verify Installation

```powershell
# Check .NET version
dotnet --version
# Should output: 10.0.xxx

# Check Aspire workload
dotnet workload list | findstr aspire
# Should show: aspire

# Check Docker
docker --version
```

---

## Quick Start

### 1. Clone and Setup

```powershell
# Navigate to project root
cd E:\PROJECTS\GITHUB\HookVerse

# Restore dependencies
dotnet restore

# Build solution
dotnet build
```

### 2. Start Everything with Aspire

```powershell
# Start AppHost - this starts everything!
dotnet run --project src\HookVerse.AppHost
```

That's it! Aspire will automatically:
- ✅ Pull and start PostgreSQL, RabbitMQ, and Redis containers
- ✅ Build and launch API, Worker, and Dashboard services  
- ✅ Configure service discovery and connection strings
- ✅ Set up OpenTelemetry observability
- ✅ Open the Aspire dashboard at http://localhost:15888

**First-time startup**: 2-3 minutes (downloading images)  
**Subsequent startups**: 30-60 seconds

### 3. Access Your Services

Once the AppHost is running:

| Service | URL | Description |
|---------|-----|-------------|
| **Aspire Dashboard** | http://localhost:15888 | Orchestration, logs, traces, metrics |
| **HookVerse API** | http://localhost:5000 | REST API endpoints |
| **API Swagger** | http://localhost:5000/swagger | API documentation |
| **HookVerse Dashboard** | http://localhost:7000 | Webhook monitoring UI |
| **RabbitMQ Management** | http://localhost:15672 | Message queue admin (guest/guest) |

### 4. Apply Database Migrations (First Time Only)

```powershell
# Ensure AppHost is running first (for database connection)

# In a new terminal:
---

## Using the Aspire Dashboard

The Aspire Dashboard (http://localhost:15888) is your command center for monitoring and managing all services.

### Dashboard Features

#### 1. Resources View

**What it shows**: All running services and containers with their status

Features:
- Real-time health status of each resource
- Start/stop/restart individual resources
- View resource allocation (CPU, memory)
- Quick access to logs and environment variables

**How to use**:
1. Navigate to http://localhost:15888
2. Click on **Resources** tab (default view)
3. See all 6 resources:
   - hookverse-api (ASP.NET Core)
   - hookverse-worker (Background Service)
   - hookverse-dashboard (Blazor App)
   - postgres (PostgreSQL container)
   - rabbitmq (RabbitMQ container)
   - redis (Redis container)

#### 2. Console Logs

**What it shows**: Real-time streaming logs from all services

Features:
- Live log streaming with auto-scroll
- Filter by service name
- Search within logs
- Copy logs to clipboard
- Export logs to file

**How to use**:
1. Go to **Console Logs** tab
2. Select a resource from the dropdown (or view all)
3. Use the search box to filter log messages
4. Click **Copy** or **Export** to save logs

#### 3. Structured Logs

**What it shows**: Queryable, filterable structured log data

Features:
- Advanced filtering by:
  - Log level (Trace, Debug, Info, Warning, Error, Critical)
  - Category (e.g., `HookVerse.Api.Controllers.WebhooksController`)
  - Time range
  - Correlation ID
- Full log message details
- Stack traces for errors
- JSON property inspection

**How to use**:
1. Go to **Structured Logs** tab
2. Set filters:
   - Resource: Select specific service
   - Level: Choose log level
   - Time: Select time range
3. Click on any log entry to see full details
4. Use correlation ID to trace requests across services

#### 4. Traces

**What it shows**: Distributed tracing across all services

Features:
- End-to-end request visualization
- Span timeline showing service call hierarchy
- Performance metrics per span
- Correlation with logs
- Identify bottlenecks and slow operations

**How to use**:
1. Go to **Traces** tab
2. Browse recent traces or search by:
   - Trace ID
   - Operation name
   - Duration
   - Status
3. Click on a trace to see the full waterfall diagram
4. Inspect individual spans for timing details
5. Click "View Logs" to see related log entries

**Example trace flow**:
```
HTTP POST /api/webhooks
├─ API: WebhooksController.Create  (12ms)
├─ API: WebhookRepository.CreateAsync  (8ms)
│  └─ PostgreSQL: INSERT INTO Webhooks  (5ms)
└─ API: RabbitMQ.Publish  (3ms)
    └─ Worker: DeliveryConsumer.Consume  (45ms)
       └─ Worker: HttpClient.SendAsync  (42ms)
```

#### 5. Metrics

**What it shows**: Real-time performance metrics and counters

Features:
- CPU and memory usage per service
- Request rates and latencies
- Database query performance
- Custom business metrics
- Historical charts

**How to use**:
1. Go to **Metrics** tab
2. Select a resource
3. Choose metric category:
   - **System**: CPU, memory, threads
   - **HTTP**: Request count, duration, status codes
   - **Database**: Query count, connection pool
   - **Custom**: Application-specific metrics
4. Adjust time window for historical data

### Common Dashboard Tasks

#### Monitor a New Webhook Delivery

1. Open **Traces** tab
2. Send a POST request to `/api/webhooks`
3. Find the new trace (sorted by recency)
4. Click to see the full flow:
   - API receives request
   - Database insert
   - RabbitMQ publish
   - Worker processing
   - HTTP delivery to destination

#### Troubleshoot a Failed Delivery

1. Go to **Structured Logs** tab
2. Filter:
   - Resource: `hookverse-worker`
   - Level: `Error`
   - Time: Last 1 hour
3. Find the error log
4. Click to view details and stack trace
5. Note the correlation ID
6. Go to **Traces** tab and search by correlation ID
7. See the full request flow to identify the failure point

#### Check Service Health

1. Go to **Resources** tab
2. Check the **State** column:
   - 🟢 **Running**: Service is healthy
   - 🟡 **Starting**: Service is initializing
   - 🔴 **Stopped**: Service has stopped
   - ⚠️ **Unhealthy**: Health check failing
3. Click on an unhealthy resource
4. View **Console Logs** for error messages
5. Check **Environment** tab for configuration issues

#### View Database Queries

1. Go to **Traces** tab
2. Filter by **Name**: `db.*` or `EntityFramework*`
3. Click on a trace to see:
   - SQL query text
   - Execution time
   - Parameter values
   - Stack trace showing which code triggered the query

---

## Debugging in Visual Studio Code

### 1. Debugging with Aspire AppHost

The recommended way to debug is through the AppHost, which provides full orchestration.

**Launch Configuration** (`.vscode/launch.json`):

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Debug Aspire AppHost",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/src/HookVerse.AppHost/bin/Debug/net10.0/HookVerse.AppHost.dll",
      "args": [],
      "cwd": "${workspaceFolder}/src/HookVerse.AppHost",
      "stopAtEntry": false,
      "env": {
        "DOTNET_ENVIRONMENT": "Development"
      },
      "serverReadyAction": {
        "action": "openExternally",
        "pattern": "Now listening on: (http://[^\\s]+)",
        "uriFormat": "%s"
      }
    }
  ]
}
```

**How to debug**:
1. Set breakpoints in your API, Worker, or Dashboard code
2. Press **F5** or click **Run > Start Debugging**
3. AppHost starts all services
4. Aspire Dashboard opens automatically
5. Trigger your code path (e.g., POST to `/api/webhooks`)
6. VS Code breaks at your breakpoint

**Advantages**:
- All services running together
- Full observability through Aspire Dashboard
- Realistic environment with service discovery
- Container dependencies managed automatically

### 2. Debugging Individual Services

If you need to debug just one service without AppHost:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Debug API Only",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/src/HookVerse.Api/bin/Debug/net10.0/HookVerse.Api.dll",
      "args": [],
      "cwd": "${workspaceFolder}/src/HookVerse.Api",
      "stopAtEntry": false,
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "ASPNETCORE_URLS": "http://localhost:5000"
      },
      "serverReadyAction": {
        "action": "openExternally",
        "pattern": "\\bNow listening on:\\s+(https?://\\S+)"
      }
    },
    {
      "name": "Debug Worker Only",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/src/HookVerse.Worker/bin/Debug/net10.0/HookVerse.Worker.dll",
      "args": [],
      "cwd": "${workspaceFolder}/src/HookVerse.Worker",
      "stopAtEntry": false,
      "env": {
        "DOTNET_ENVIRONMENT": "Development"
      }
    },
    {
      "name": "Debug Dashboard Only",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/src/HookVerse.Dashboard/bin/Debug/net10.0/HookVerse.Dashboard.dll",
      "args": [],
      "cwd": "${workspaceFolder}/src/HookVerse.Dashboard",
      "stopAtEntry": false,
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "ASPNETCORE_URLS": "http://localhost:7000"
      },
      "serverReadyAction": {
        "action": "openExternally",
        "pattern": "\\bNow listening on:\\s+(https?://\\S+)"
      }
    }
  ]
}
```

**Note**: When debugging individual services, you still need AppHost running in a separate terminal for infrastructure (PostgreSQL, RabbitMQ, Redis).

### 3. Build Tasks (`.vscode/tasks.json`)

```json
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "build",
      "command": "dotnet",
      "type": "process",
      "args": [
        "build",
        "${workspaceFolder}/HookVerse.sln",
        "/property:GenerateFullPaths=true",
        "/consoleloggerparameters:NoSummary"
      ],
      "problemMatcher": "$msCompile"
    },
    {
      "label": "Start AppHost",
      "command": "dotnet",
      "type": "process",
      "args": [
        "run",
        "--project",
        "${workspaceFolder}/src/HookVerse.AppHost"
      ],
      "isBackground": true,
      "problemMatcher": {
        "pattern": {
          "regexp": "^.*$",
          "file": 1,
          "location": 2,
          "message": 3
        },
        "background": {
          "activeOnStart": true,
          "beginsPattern": "^.*Building.*$",
          "endsPattern": "^.*Application started.*$"
        }
      }
    }
  ]
}
```

---

## Debugging in Visual Studio

### 1. Debug with Aspire AppHost

**Steps**:
1. Open `HookVerse.sln` in Visual Studio 2022
2. Set `HookVerse.AppHost` as the startup project (right-click → Set as Startup Project)
3. Set breakpoints in any service (API, Worker, Dashboard)
4. Press **F5** or click **Start Debugging**
5. Visual Studio will:
   - Build all projects
   - Start AppHost
   - Launch Aspire Dashboard in browser
   - Attach debugger to all services

### 2. Debug Multiple Projects Simultaneously

**Configure Multiple Startup Projects**:
1. Right-click solution → **Properties**
2. Select **Multiple startup projects**
3. Set **Start** for:
   - HookVerse.AppHost
   - HookVerse.Api (optional - if you want F10/F11 stepping)
   - HookVerse.Worker (optional)
4. Click **OK**
5. Press **F5**

### 3. Attach to Running Process

If AppHost is already running:
1. **Debug** → **Attach to Process** (Ctrl+Alt+P)
2. Filter by "HookVerse"
3. Select the service process (e.g., `HookVerse.Api.exe`)
4. Click **Attach**

---

## Using the API

### Swagger UI

Once the AppHost is running, access Swagger at: http://localhost:5000/swagger

**Key Endpoints**:

#### Create a Webhook
```http
POST /api/webhooks
Content-Type: application/json

{
  "name": "My Test Webhook",
  "url": "https://webhook.site/unique-id",
  "events": ["user.created", "order.placed"],
  "secret": "my-secret-key",
  "isActive": true
}
```

#### List Webhooks
```http
GET /api/webhooks?pageNumber=1&pageSize=20
```

#### Get Webhook by ID
```http
GET /api/webhooks/{id}
```

#### Update Webhook
```http
PUT /api/webhooks/{id}
Content-Type: application/json

{
  "name": "Updated Webhook",
  "url": "https://new-url.example.com",
  "isActive": true
}
```

#### Delete Webhook
```http
DELETE /api/webhooks/{id}
```

#### Trigger a Test Delivery
```http
POST /api/webhooks/{id}/test
Content-Type: application/json

{
  "eventType": "test.event",
  "payload": {
    "message": "This is a test"
  }
}
```

### Using cURL

```powershell
# Create webhook
curl -X POST http://localhost:5000/api/webhooks `
  -H "Content-Type: application/json" `
  -d '{"name":"Test Webhook","url":"https://webhook.site/abc123","events":["test.event"]}'

# List webhooks
curl http://localhost:5000/api/webhooks

# Get webhook
curl http://localhost:5000/api/webhooks/1

# Delete webhook
curl -X DELETE http://localhost:5000/api/webhooks/1
```

### Using PowerShell

```powershell
# Create webhook
$body = @{
    name = "Test Webhook"
    url = "https://webhook.site/abc123"
    events = @("test.event")
    secret = "my-secret"
    isActive = $true
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/api/webhooks" `
    -Method Post `
    -ContentType "application/json" `
    -Body $body

# List webhooks
Invoke-RestMethod -Uri "http://localhost:5000/api/webhooks"

# Get webhook
$webhook = Invoke-RestMethod -Uri "http://localhost:5000/api/webhooks/1"
$webhook | ConvertTo-Json

# Delete webhook
Invoke-RestMethod -Uri "http://localhost:5000/api/webhooks/1" -Method Delete
```

---

## Using the Dashboard

Access the HookVerse monitoring dashboard at: http://localhost:7000

### Features

#### 1. Webhook Overview
- Total webhooks registered
- Active vs inactive webhooks
- Recent webhook creations

#### 2. Delivery Statistics
- Total deliveries attempted
- Success rate
- Failure rate
- Average delivery time

#### 3. Real-time Activity
- Live delivery feed
- Recent successes and failures
- Retry attempts

#### 4. Webhook Details
Click on any webhook to see:
- Configuration (URL, events, secret)
- Delivery history
- Success/failure trends
- Recent payloads

---

## Troubleshooting

### AppHost Won't Start

**Symptom**: AppHost fails to start or exits immediately

**Solutions**:
1. **Check Docker is running**:
   ```powershell
   docker ps
   ```
   If this fails, start Docker Desktop

2. **Check Aspire workload**:
   ```powershell
   dotnet workload list | findstr aspire
   ```
   If missing, install it:
   ```powershell
   dotnet workload install aspire
   ```

3. **Check for port conflicts**:
   ```powershell
   # Check if port 15888 (Aspire Dashboard) is in use
   netstat -ano | findstr :15888
   
   # Check API port
   netstat -ano | findstr :5000
   ```

4. **Clean and rebuild**:
   ```powershell
   dotnet clean
   dotnet build
   dotnet run --project src\HookVerse.AppHost
   ```

### Container Won't Start

**Symptom**: PostgreSQL, RabbitMQ, or Redis container fails in Aspire Dashboard

**Solutions**:
1. **View logs in Aspire Dashboard**:
   - Go to http://localhost:15888
   - Click **Resources** tab
   - Click on the failing resource
   - View **Console Logs** for error messages

2. **Check Docker Desktop**:
   - Open Docker Desktop
   - Check "Containers" tab
   - Look for HookVerse-related containers
   - View logs for failed containers

3. **Remove and restart**:
   ```powershell
   # Stop AppHost (Ctrl+C)
   
   # Remove containers
   docker ps -a | findstr hookverse | ForEach-Object { docker rm -f $_.Split()[0] }
   
   # Start AppHost again
   dotnet run --project src\HookVerse.AppHost
   ```

### Service Discovery Not Working

**Symptom**: API can't connect to PostgreSQL, RabbitMQ, or Redis

**Solutions**:
1. **Verify in Aspire Dashboard**:
   - Go to **Resources** tab
   - Check all resources show "Running"
   - Click on a resource → **Environment** tab
   - Verify connection string environment variables are set

2. **Check service logs**:
   - Go to **Structured Logs** tab
   - Filter by the failing service
   - Look for connection errors

3. **Restart AppHost**:
   ```powershell
   # Ctrl+C to stop
   dotnet run --project src\HookVerse.AppHost
   ```

### Database Migration Fails

**Symptom**: `dotnet ef database update` fails

**Solutions**:
1. **Ensure AppHost is running**:
   ```powershell
   # In terminal 1:
   dotnet run --project src\HookVerse.AppHost
   
   # Wait for all services to start (check Aspire Dashboard)
   
   # In terminal 2:
   cd src\HookVerse.Infrastructure
   dotnet ef database update --startup-project ..\HookVerse.Api
   ```

2. **Check PostgreSQL is healthy**:
   - Open Aspire Dashboard
   - Check PostgreSQL resource is "Running"
   - View logs for any errors

3. **Manual connection test**:
   ```powershell
   # Find PostgreSQL container
   docker ps | findstr postgres
   
   # Connect to database
   docker exec -it <container-name> psql -U hookverse -d hookverse
   ```

### Worker Not Processing Messages

**Symptom**: Webhooks created but no deliveries happening

**Solutions**:
1. **Check Worker logs in Aspire Dashboard**:
   - Go to **Console Logs** tab
   - Select `hookverse-worker`
   - Look for "Connected to RabbitMQ" message
   - Check for consumption errors

2. **Verify RabbitMQ connection**:
   - Open RabbitMQ Management UI: http://localhost:15672
   - Login: guest/guest
   - Check **Connections** tab
   - Verify Worker is connected

3. **Check queue status**:
   - In RabbitMQ Management UI
   - Go to **Queues** tab
   - Look for `webhook-deliveries` queue
   - Check message count

4. **Restart Worker**:
   - In Aspire Dashboard → **Resources** tab
   - Click on `hookverse-worker`
   - Click **Restart** button

### Debugging Not Working

**Symptom**: Breakpoints not hit in VS Code or Visual Studio

**Solutions**:
1. **Verify debugger is attached**:
   - In VS Code: Check debug toolbar shows "Pause/Continue" buttons
   - In Visual Studio: Check "Debug" menu shows "Continue" (not "Start Debugging")

2. **Check build configuration**:
   ```powershell
   # Ensure Debug build (not Release)
   dotnet build --configuration Debug
   ```

3. **Set breakpoint correctly**:
   - Place breakpoint in code that will actually execute
   - Trigger the code path (e.g., POST to API endpoint)
   - Check Aspire Dashboard **Traces** to verify request reached the service

4. **Rebuild and restart**:
   - Stop debugging (Shift+F5)
   - Clean solution: `dotnet clean`
   - Build: `dotnet build`
   - Start debugging (F5)

### Performance Issues

**Symptom**: Slow startup or sluggish responses

**Solutions**:
1. **Check Docker resources**:
   - Docker Desktop → Settings → Resources
   - Ensure at least:
     - Memory: 4 GB (8 GB recommended)
     - CPUs: 2 (4 recommended)

2. **View metrics in Aspire Dashboard**:
   - Go to **Metrics** tab
   - Check CPU and memory usage per service
   - Identify bottlenecks

3. **Check trace timings**:
   - Go to **Traces** tab
   - Look for slow operations
   - Identify database queries or HTTP calls taking too long

4. **Optimize database**:
   ```sql
   -- Connect to PostgreSQL
   docker exec -it <postgres-container> psql -U hookverse -d hookverse
   
   -- Check table sizes
   SELECT schemaname, tablename, 
          pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS size
   FROM pg_tables
   WHERE schemaname = 'public'
   ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;
   ```

---

## Additional Resources

- **Aspire Documentation**: https://learn.microsoft.com/dotnet/aspire
- **HookVerse API Guide**: [API-GUIDE.md](API-GUIDE.md)
- **Development Guide**: [DEVELOPMENT.md](DEVELOPMENT.md)
- **Deployment Guide**: [DEPLOYMENT.md](DEPLOYMENT.md)
- **Troubleshooting**: [docs/troubleshooting-aspire.md](docs/troubleshooting-aspire.md)

---

## Quick Reference

### Essential Commands

```powershell
# Start everything
dotnet run --project src\HookVerse.AppHost

# Apply migrations (AppHost must be running)
cd src\HookVerse.Infrastructure
dotnet ef database update --startup-project ..\HookVerse.Api

# Run tests
dotnet test

# Build solution
dotnet build

# Clean solution
dotnet clean
```

### Essential URLs

| Service | URL | Credentials |
|---------|-----|-------------|
| Aspire Dashboard | http://localhost:15888 | - |
| API | http://localhost:5000 | - |
| API Swagger | http://localhost:5000/swagger | - |
| Dashboard | http://localhost:7000 | - |
| RabbitMQ Mgmt | http://localhost:15672 | guest/guest |

### Key Directories

| Directory | Purpose |
|-----------|---------|
| `src/HookVerse.AppHost` | Aspire orchestration |
| `src/HookVerse.Api` | REST API |
| `src/HookVerse.Worker` | Background worker |
| `src/HookVerse.Dashboard` | Monitoring UI |
| `tests/` | All test projects |
| `docs/` | Documentation |

---

**Need Help?** Check the [Troubleshooting Guide](docs/troubleshooting-aspire.md) or open an issue on GitHub.