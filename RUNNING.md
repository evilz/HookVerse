# HookVerse - Running and Debugging Guide

**Last Updated**: October 19, 2025  
**Version**: 1.0.0

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Quick Start](#quick-start)
3. [Running the Application](#running-the-application)
4. [Debugging in Visual Studio Code](#debugging-in-visual-studio-code)
5. [Debugging in Visual Studio](#debugging-in-visual-studio)
6. [Using the API](#using-the-api)
7. [Using the Dashboard](#using-the-dashboard)
8. [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Required Software

- **.NET 10 SDK** (preview) - [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Docker Desktop** - [Download](https://www.docker.com/products/docker-desktop)
- **PostgreSQL 16** (via Docker or local install)
- **RabbitMQ 3.13** (via Docker or local install)
- **Redis 7** (via Docker or local install)

### Optional Tools

- **Visual Studio Code** with C# Dev Kit extension
- **Visual Studio 2022** (17.8+)
- **Postman** or **Thunder Client** for API testing
- **pgAdmin 4** for database management (included in Docker setup)

### Verify Installation

```powershell
# Check .NET version
dotnet --version
# Should output: 10.0.xxx

# Check Docker
docker --version
docker compose version
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

### 2. Start Infrastructure Services

**Option A: Using Docker Compose (Recommended)**

```powershell
# Start all services (PostgreSQL, RabbitMQ, Redis)
.\start-services.ps1

# Or manually:
docker compose up -d

# Verify services are running
docker compose ps
```

Services will be available at:
- **PostgreSQL**: localhost:5432
- **RabbitMQ Management**: http://localhost:15672 (guest/guest)
- **Redis**: localhost:6379
- **pgAdmin**: http://localhost:5050 (admin@hookverse.local/admin)

**Option B: Local Services**

If you prefer local installations, ensure these are running:
- PostgreSQL on port 5432
- RabbitMQ on port 5672 (management on 15672)
- Redis on port 6379

### 3. Apply Database Migrations

```powershell
# From Infrastructure project
cd src\HookVerse.Infrastructure

# Create/update database
dotnet ef database update --startup-project ..\HookVerse.Api

# Verify migration
dotnet ef migrations list --startup-project ..\HookVerse.Api
```

### 4. Run the Applications

**Terminal 1: API Server**

```powershell
cd src\HookVerse.Api
dotnet run
```

API will start at:
- **HTTPS**: https://localhost:7001
- **HTTP**: http://localhost:5001
- **Swagger UI**: https://localhost:7001/swagger

**Terminal 2: Background Worker**

```powershell
cd src\HookVerse.Worker
dotnet run
```

Worker will connect to RabbitMQ and process webhook deliveries.

**Terminal 3: Dashboard (Optional)**

```powershell
cd src\HookVerse.Dashboard
dotnet run
```

Dashboard will start at:
- **HTTPS**: https://localhost:7002
- **HTTP**: http://localhost:5002

---

## Running the Application

### Using .NET CLI

**Run All Projects Simultaneously (PowerShell)**

```powershell
# Create a multi-run script
$api = Start-Process pwsh -ArgumentList "-NoExit", "-Command", "cd src\HookVerse.Api; dotnet run" -PassThru
$worker = Start-Process pwsh -ArgumentList "-NoExit", "-Command", "cd src\HookVerse.Worker; dotnet run" -PassThru
$dashboard = Start-Process pwsh -ArgumentList "-NoExit", "-Command", "cd src\HookVerse.Dashboard; dotnet run" -PassThru

Write-Host "All services started!"
Write-Host "API: https://localhost:7001"
Write-Host "Dashboard: https://localhost:7002"
Write-Host "Press Ctrl+C to stop monitoring..."

# Wait and cleanup
try {
    while ($true) { Start-Sleep -Seconds 1 }
} finally {
    Stop-Process -Id $api.Id -ErrorAction SilentlyContinue
    Stop-Process -Id $worker.Id -ErrorAction SilentlyContinue
    Stop-Process -Id $dashboard.Id -ErrorAction SilentlyContinue
}
```

**Run with Watch Mode (Auto-reload on changes)**

```powershell
# API with hot reload
cd src\HookVerse.Api
dotnet watch run

# Worker with hot reload
cd src\HookVerse.Worker
dotnet watch run
```

---

## Debugging in Visual Studio Code

### 1. Launch Configuration

Create `.vscode/launch.json`:

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
      "args": [],
      "cwd": "${workspaceFolder}/src/HookVerse.Api",
      "stopAtEntry": false,
      "serverReadyAction": {
        "action": "openExternally",
        "pattern": "\\bNow listening on:\\s+(https?://\\S+)"
      },
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "ASPNETCORE_URLS": "https://localhost:7001;http://localhost:5001"
      },
      "sourceFileMap": {
        "/Views": "${workspaceFolder}/Views"
      }
    },
    {
      "name": "Launch Worker",
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
      "name": "Launch Dashboard",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/src/HookVerse.Dashboard/bin/Debug/net10.0/HookVerse.Dashboard.dll",
      "args": [],
      "cwd": "${workspaceFolder}/src/HookVerse.Dashboard",
      "stopAtEntry": false,
      "serverReadyAction": {
        "action": "openExternally",
        "pattern": "\\bNow listening on:\\s+(https?://\\S+)"
      },
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "ASPNETCORE_URLS": "https://localhost:7002;http://localhost:5002"
      }
    }
  ],
  "compounds": [
    {
      "name": "All Services",
      "configurations": ["Launch API", "Launch Worker", "Launch Dashboard"],
      "stopAll": true
    }
  ]
}
```

### 2. Tasks Configuration

Create `.vscode/tasks.json`:

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
      "label": "watch-api",
      "command": "dotnet",
      "type": "process",
      "args": [
        "watch",
        "run",
        "--project",
        "${workspaceFolder}/src/HookVerse.Api"
      ],
      "problemMatcher": "$msCompile"
    }
  ]
}
```

### 3. Start Debugging

1. Press `F5` or click "Run and Debug" in sidebar
2. Select "All Services" from dropdown to run everything
3. Or select individual services (API, Worker, Dashboard)
4. Set breakpoints by clicking in the gutter (left of line numbers)
5. Use Debug Console to inspect variables

**Debug Shortcuts:**
- `F5` - Continue/Start
- `F10` - Step Over
- `F11` - Step Into
- `Shift+F11` - Step Out
- `Ctrl+Shift+F5` - Restart
- `Shift+F5` - Stop

---

## Debugging in Visual Studio

### 1. Open Solution

```powershell
# Open Visual Studio
start HookVerse.sln
```

### 2. Configure Multiple Startup Projects

1. Right-click Solution → **Properties**
2. Select **Multiple startup projects**
3. Set these to **Start**:
   - HookVerse.Api
   - HookVerse.Worker
   - HookVerse.Dashboard (optional)

### 3. Start Debugging

1. Press `F5` or click "Start"
2. All configured projects will launch
3. Set breakpoints by clicking in margin
4. Use Immediate Window (`Ctrl+Alt+I`) for runtime evaluation

**Debug Windows:**
- **Locals** (`Ctrl+Alt+V, L`) - Variables in current scope
- **Watch** (`Ctrl+Alt+W, 1-4`) - Watch expressions
- **Call Stack** (`Ctrl+Alt+C`) - Execution stack
- **Output** (`Ctrl+Alt+O`) - Application logs

---

## Using the API

### 1. Access Swagger UI

Open browser to: https://localhost:7001/swagger

Swagger provides interactive API documentation with "Try it out" functionality.

### 2. API Endpoints Overview

#### **Subscribers** (`/api/v1/subscribers`)

```http
# Create a subscriber
POST /api/v1/subscribers
Content-Type: application/json

{
  "name": "Acme Corp",
  "apiKeyPrefix": "acme",
  "webhookSecret": "32-character-secret-key-here!!!",
  "retentionDays": 30
}

# Response: 201 Created
{
  "id": "guid",
  "name": "Acme Corp",
  "apiKey": "acme_live_abc123...",
  "isActive": true,
  "createdAt": "2025-10-19T10:00:00Z"
}
```

#### **Event Types** (`/api/v1/event-types`)

```http
# Create an event type
POST /api/v1/event-types
Content-Type: application/json

{
  "name": "user.created",
  "description": "Triggered when a new user is created",
  "subscriberId": "subscriber-guid-here"
}

# Attach JSON schema
PUT /api/v1/event-types/user.created/schema
Content-Type: application/json

{
  "format": "JsonSchema",
  "content": "{\"type\":\"object\",\"properties\":{\"userId\":{\"type\":\"string\"},\"email\":{\"type\":\"string\"}}}",
  "description": "User creation event schema",
  "version": 1,
  "isActive": true
}
```

#### **Subscriptions** (`/api/v1/subscriptions`)

```http
# Create a subscription
POST /api/v1/subscriptions
Content-Type: application/json

{
  "eventTypeId": "event-type-guid",
  "endpointUrl": "https://your-app.com/webhooks",
  "secret": "your-webhook-signing-secret-32-chars",
  "authType": "None",
  "description": "Production webhook endpoint",
  "timeoutSeconds": 30,
  "maxRetries": 5
}
```

#### **Webhooks** (`/api/v1/webhooks`)

```http
# Send a webhook
POST /api/v1/webhooks
Content-Type: application/json

{
  "eventTypeId": "event-type-guid",
  "subscriberId": "subscriber-guid",
  "payload": "{\"userId\":\"123\",\"email\":\"user@example.com\"}",
  "scheduledFor": null,
  "metadata": "{\"source\":\"api\"}"
}

# Response: 202 Accepted
{
  "id": "webhook-event-guid",
  "traceId": "abc123...",
  "status": "accepted",
  "createdAt": "2025-10-19T10:05:00Z"
}
```

### 3. Using cURL

```bash
# Set variables
API_URL="https://localhost:7001"
API_KEY="your-api-key-here"

# Create subscriber
curl -X POST "$API_URL/api/v1/subscribers" \
  -H "Content-Type: application/json" \
  -H "X-API-Key: $API_KEY" \
  -d '{
    "name": "Test Subscriber",
    "apiKeyPrefix": "test",
    "webhookSecret": "test-secret-32-characters-long!",
    "retentionDays": 30
  }' \
  --insecure

# Send webhook
curl -X POST "$API_URL/api/v1/webhooks" \
  -H "Content-Type: application/json" \
  -H "X-API-Key: $API_KEY" \
  -d '{
    "eventTypeId": "event-type-guid",
    "subscriberId": "subscriber-guid",
    "payload": "{\"test\":\"data\"}"
  }' \
  --insecure
```

### 4. Using PowerShell

```powershell
# Set variables
$apiUrl = "https://localhost:7001"
$headers = @{
    "Content-Type" = "application/json"
    "X-API-Key" = "your-api-key-here"
}

# Create subscriber
$body = @{
    name = "Test Subscriber"
    apiKeyPrefix = "test"
    webhookSecret = "test-secret-32-characters-long!"
    retentionDays = 30
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "$apiUrl/api/v1/subscribers" `
    -Method Post `
    -Headers $headers `
    -Body $body `
    -SkipCertificateCheck

Write-Host "Created subscriber: $($response.id)"

# Send webhook
$webhookBody = @{
    eventTypeId = "event-type-guid"
    subscriberId = "subscriber-guid"
    payload = '{"userId":"123","email":"user@example.com"}'
} | ConvertTo-Json

$webhookResponse = Invoke-RestMethod -Uri "$apiUrl/api/v1/webhooks" `
    -Method Post `
    -Headers $headers `
    -Body $webhookBody `
    -SkipCertificateCheck

Write-Host "Webhook sent with TraceId: $($webhookResponse.traceId)"
```

### 5. Testing with Postman

**Import Collection:**

1. Open Postman
2. Click **Import**
3. Create new collection "HookVerse API"
4. Add environment variables:
   - `base_url`: https://localhost:7001
   - `api_key`: your-api-key

**Example Requests:**

```json
// Collection structure:
HookVerse API/
├── Subscribers/
│   ├── Create Subscriber (POST)
│   ├── List Subscribers (GET)
│   └── Get Subscriber (GET)
├── Event Types/
│   ├── Create Event Type (POST)
│   ├── Attach Schema (PUT)
│   └── List Event Types (GET)
├── Subscriptions/
│   ├── Create Subscription (POST)
│   ├── List Subscriptions (GET)
│   └── Update Subscription (PUT)
└── Webhooks/
    ├── Send Webhook (POST)
    └── Get Webhook Status (GET)
```

---

## Using the Dashboard

### 1. Access Dashboard

Open browser to: https://localhost:7002

### 2. Dashboard Features (When Completed)

- **Webhooks List** - View all webhook events with filtering
- **Webhook Details** - Inspect delivery attempts, request/response
- **Analytics** - Success rates, latency charts, daily trends
- **Real-time Updates** - Live delivery notifications via SignalR

### 3. Current Status

🚧 **Dashboard is partially implemented:**
- ✅ API client services (T152-T154)
- ⏳ Blazor components (T145-T148) - **Not yet implemented**
- ⏳ Blazor pages (T149-T151) - **Not yet implemented**
- ⏳ API endpoints (T155-T158) - **Not yet implemented**
- ⏳ Real-time features (T159-T162) - **Not yet implemented**

---

## Troubleshooting

### Database Connection Issues

**Error**: "Could not connect to PostgreSQL"

```powershell
# Check if PostgreSQL is running
docker compose ps postgres

# View logs
docker compose logs postgres

# Restart PostgreSQL
docker compose restart postgres

# Test connection
docker exec -it hookverse-postgres psql -U hookverse -d hookverse
```

### RabbitMQ Connection Issues

**Error**: "Connection refused to RabbitMQ"

```powershell
# Check if RabbitMQ is running
docker compose ps rabbitmq

# View logs
docker compose logs rabbitmq

# Access management UI
start http://localhost:15672
# Login: guest/guest

# Restart RabbitMQ
docker compose restart rabbitmq
```

### Port Already in Use

**Error**: "Address already in use: 7001"

```powershell
# Find process using port
netstat -ano | findstr :7001

# Kill process (use PID from above)
Stop-Process -Id <PID> -Force

# Or change port in appsettings.json
```

### Migration Issues

**Error**: "Pending migrations"

```powershell
cd src\HookVerse.Infrastructure

# List migrations
dotnet ef migrations list --startup-project ..\HookVerse.Api

# Apply migrations
dotnet ef database update --startup-project ..\HookVerse.Api

# Reset database (WARNING: Data loss!)
dotnet ef database drop --startup-project ..\HookVerse.Api --force
dotnet ef database update --startup-project ..\HookVerse.Api
```

### SSL Certificate Issues

**Error**: "SSL connection could not be established"

```powershell
# Trust development certificate
dotnet dev-certs https --trust

# Or use HTTP (not recommended for production)
# Change URLs in appsettings.json to http://
```

### Worker Not Processing Webhooks

**Check Worker is Running:**

```powershell
# Worker should show:
# "Connected to RabbitMQ"
# "Consumer started for queue: webhook-delivery-queue"

# If not, check RabbitMQ connection in appsettings.json
```

**Check Queue:**

1. Open RabbitMQ Management: http://localhost:15672
2. Navigate to **Queues** tab
3. Look for `webhook-delivery-queue`
4. Check message count and consumer count

### API Returns 500 Error

**Check Logs:**

```powershell
# API logs are output to console
# Look for stack traces and error messages

# Enable detailed logging in appsettings.Development.json:
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  }
}
```

### Docker Services Not Starting

```powershell
# Stop all containers
docker compose down

# Remove volumes (WARNING: Data loss!)
docker compose down -v

# Rebuild and restart
docker compose up -d --build

# Check disk space
docker system df

# Clean up unused resources
docker system prune -a --volumes
```

---

## Quick Reference

### Essential URLs

| Service | URL | Credentials |
|---------|-----|-------------|
| API (HTTPS) | https://localhost:7001 | - |
| API Swagger | https://localhost:7001/swagger | - |
| Dashboard (HTTPS) | https://localhost:7002 | - |
| RabbitMQ Management | http://localhost:15672 | guest/guest |
| pgAdmin | http://localhost:5050 | admin@hookverse.local/admin |
| PostgreSQL | localhost:5432 | hookverse/hookverse123 |
| Redis | localhost:6379 | - |

### Common Commands

```powershell
# Start infrastructure
docker compose up -d

# Stop infrastructure
docker compose down

# Build solution
dotnet build

# Run tests
dotnet test

# Apply migrations
cd src\HookVerse.Infrastructure
dotnet ef database update --startup-project ..\HookVerse.Api

# Run API
cd src\HookVerse.Api
dotnet run

# Run Worker
cd src\HookVerse.Worker
dotnet run

# Clean build
dotnet clean
dotnet build
```

### Environment Variables

```bash
# .env file for Docker Compose
POSTGRES_USER=hookverse
POSTGRES_PASSWORD=hookverse123
POSTGRES_DB=hookverse
RABBITMQ_DEFAULT_USER=guest
RABBITMQ_DEFAULT_PASS=guest
REDIS_PASSWORD=
```

---

## Next Steps

1. ✅ **Infrastructure is Running** - Docker services are up
2. ✅ **Database is Ready** - Migrations applied
3. ✅ **API is Responding** - Swagger UI accessible
4. ✅ **Worker is Processing** - Connected to RabbitMQ
5. 🚧 **Dashboard Pending** - Awaiting component implementation

For more information, see:
- [DEVELOPMENT.md](./DEVELOPMENT.md) - Development workflow
- [DOCKER.md](./DOCKER.md) - Docker infrastructure details
- [API Documentation](https://localhost:7001/swagger) - Interactive API docs

---

**Questions or Issues?**

Open an issue in the repository or check the troubleshooting section above.
