# HookVerse Development Environment Setup

This guide will help you set up a complete development environment for HookVerse using .NET Aspire.

## Prerequisites

Before you begin, ensure you have the following installed:

- **.NET 10 SDK** (preview): [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
- **.NET Aspire workload**: Install with `dotnet workload install aspire`
- **Docker Desktop**: [Download](https://www.docker.com/products/docker-desktop/) (for container dependencies)
- **Your favorite IDE**: Visual Studio 2022 (preview), VS Code, or Rider

### Verify Installation

```bash
# Check .NET SDK
dotnet --version  # Should be 10.0.x

# Check Aspire workload
dotnet workload list | findstr aspire  # Should show aspire workload

# Check Docker
docker --version  # Should be running
```

## Quick Start (Recommended)

### Single Command Start

```bash
# Start everything with Aspire
dotnet run --project src/HookVerse.AppHost
```

That's it! Aspire will automatically:
- ✅ Pull and start PostgreSQL, RabbitMQ, and Redis containers
- ✅ Build and launch API, Worker, and Dashboard services
- ✅ Configure service discovery and health checks
- ✅ Set up OpenTelemetry observability
- ✅ Open the Aspire dashboard at http://localhost:15888

### First-Time Setup

On first run, Aspire will:
1. Pull container images (PostgreSQL, RabbitMQ, Redis)
2. Create Docker networks
3. Initialize databases
4. Start all services with proper dependencies

**Estimated time**: 2-3 minutes on first run, 30-60 seconds on subsequent runs

## Access Points

Once the AppHost is running, you can access:

| Service | URL | Description |
|---------|-----|-------------|
| **Aspire Dashboard** | http://localhost:15888 | Orchestration, logs, traces, metrics |
| **HookVerse API** | http://localhost:5000 | REST API endpoints |
| **API Swagger** | http://localhost:5000/swagger | API documentation |
| **HookVerse Dashboard** | http://localhost:7000 | Webhook monitoring UI |

### Infrastructure (Managed by Aspire)

| Service | Connection | Auto-configured |
|---------|------------|-----------------|
| **PostgreSQL** | localhost:5432 | ✅ Via service discovery |
| **RabbitMQ** | localhost:5672 | ✅ Via service discovery |
| **RabbitMQ Management** | http://localhost:15672 | guest/guest |
| **Redis** | localhost:6379 | ✅ Via service discovery |

> **Note**: Connection strings are automatically injected by Aspire. No manual configuration needed!

## Development Workflow with Aspire

### Daily Workflow

1. **Start everything with one command**:
   ```bash
   dotnet run --project src/HookVerse.AppHost
   ```

2. **Open Aspire Dashboard**: http://localhost:15888
   - View all services and their status
   - Monitor logs in real-time
   - View distributed traces
   - Check metrics and health status

3. **Make code changes**: Hot reload is automatic
   - API, Worker, and Dashboard support hot reload
   - Changes are reflected immediately (< 5 seconds)
   - No need to restart services

4. **Stop everything**: Press `Ctrl+C` in the AppHost terminal
   - Gracefully stops all services
   - Containers are stopped automatically

### Running Services Individually

If you need to debug a specific service:

```bash
# Run just the API (requires AppHost for dependencies)
dotnet run --project src/HookVerse.Api

# Run just the Worker (requires AppHost for dependencies)
dotnet run --project src/HookVerse.Worker

# Run just the Dashboard (requires AppHost for dependencies)
dotnet run --project src/HookVerse.Dashboard
```

> **Tip**: Keep the AppHost running in one terminal to provide infrastructure, then run individual services in other terminals for debugging.

### Running Tests

```bash
# All tests
dotnet test

# Aspire integration tests
dotnet test tests/HookVerse.AppHost.Tests/

# API integration tests
dotnet test tests/HookVerse.Integration.Tests/

# Unit tests
dotnet test tests/HookVerse.Core.Tests/

# With coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### Database Migrations

Aspire handles database connections automatically, but you still need to create and apply migrations:

**Create a new migration:**
```bash
cd src/HookVerse.Infrastructure
dotnet ef migrations add MigrationName --startup-project ../HookVerse.Api
```

**Apply migrations:**
```bash
cd src/HookVerse.Infrastructure
dotnet ef database update --startup-project ../HookVerse.Api
```

**Rollback migration:**
```bash
dotnet ef database update PreviousMigrationName --startup-project ../HookVerse.Api
```

## Aspire Dashboard Features

The Aspire Dashboard (http://localhost:15888) provides:

### 1. Resources View
- See all services and containers
- Check health status
- View resource allocation
- Start/stop individual resources

### 2. Console Logs
- Real-time log streaming from all services
- Filter by service
- Search logs
- Export logs

### 3. Structured Logs
- Query structured logs with filters
- View log levels and categories
- Correlation ID tracking
- Timestamp filtering

### 4. Traces
- Distributed tracing across services
- View trace timelines
- Inspect spans and dependencies
- Debug performance issues

### 5. Metrics
- CPU, memory, and network metrics
- Request rates and latencies
- Custom business metrics
- Real-time dashboards

## Troubleshooting

### AppHost Won't Start

**Check Docker is running:**
```bash
docker ps  # Should list running containers
```

**Check Aspire workload:**
```bash
dotnet workload list | findstr aspire
```

**Clean and rebuild:**
```bash
dotnet clean
dotnet build
```

### Port Already in Use

If you see port conflict errors:

```bash
# Find process using port 5000
netstat -ano | findstr :5000

# Kill the process (Windows)
taskkill /PID <PID> /F

# Or use different ports in AppHost configuration
```

### Container Won't Start

**View container logs in Aspire Dashboard:**
1. Open http://localhost:15888
2. Go to Resources tab
3. Click on the failing resource
4. View logs and error messages

**Manual container check:**
```bash
# List all containers
docker ps -a

# View specific container logs
docker logs <container-name>

# Remove and restart
docker rm -f <container-name>
```

### Reset Everything

```bash
# Stop AppHost (Ctrl+C)

# Remove all HookVerse containers
docker ps -a | findstr hookverse | foreach { docker rm -f $_.Split()[0] }

# Clean solution
dotnet clean

# Start fresh
dotnet run --project src/HookVerse.AppHost
```

### Service Discovery Not Working

1. **Verify in Aspire Dashboard**: Check all services are running
2. **Check logs**: Look for connection errors in Console Logs tab
3. **Restart AppHost**: Sometimes a fresh start resolves discovery issues

### Database Migration Issues

```bash
# Ensure AppHost is running (for database connection)
dotnet run --project src/HookVerse.AppHost

# In another terminal, apply migrations
cd src/HookVerse.Infrastructure
dotnet ef database update --startup-project ../HookVerse.Api
```

## Performance Tips

### Docker Desktop Settings

Recommended settings for optimal performance:

- **Memory**: 4 GB minimum, 8 GB recommended
- **CPUs**: 2 minimum, 4 recommended
- **Disk**: 50 GB recommended

### Aspire Dashboard Performance

The Aspire Dashboard is optimized for development. If you experience slow UI:

1. Clear browser cache
2. Restart AppHost
3. Check Docker resource allocation

## Additional Tools

### RabbitMQ Management UI

Access: http://localhost:15672

Features:
- View queues and exchanges
- Monitor message rates
- Manage virtual hosts
- View connections and channels

Default credentials: guest/guest

### PostgreSQL Database Access

```bash
# Connect via docker
docker exec -it <postgres-container> psql -U hookverse -d hookverse

# Common queries
\dt          -- List tables
\d tablename -- Describe table
SELECT * FROM "Webhooks" LIMIT 10;
```

### Redis CLI

```bash
# Connect to Redis
docker exec -it <redis-container> redis-cli

# Common commands
> PING
> INFO
> KEYS *
> GET key
> SET key value
```

## Next Steps

Once your environment is running:

1. ✅ All services healthy in Aspire Dashboard
2. ✅ Migrations applied
3. ✅ Applications started
4. 📖 Read the [API Documentation](http://localhost:5000/swagger)
5. 🧪 Run the test suite: `dotnet test`
6. 📊 Explore the Aspire Dashboard: http://localhost:15888
7. 🔍 Check RabbitMQ queues: http://localhost:15672

## Getting Help

- 📚 Aspire documentation: https://learn.microsoft.com/dotnet/aspire
- 📖 API documentation: http://localhost:5000/swagger
- 🐛 Issues: https://github.com/evilz/HookVerse/issues
- 💬 Discussions: https://github.com/evilz/HookVerse/discussions
