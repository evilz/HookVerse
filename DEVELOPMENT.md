# HookVerse Development Environment Setup

This guide will help you set up a complete development environment for HookVerse.

## Quick Start (Recommended)

### Windows (PowerShell)
```powershell
.\start-services.ps1
```

### Linux/Mac (Bash)
```bash
chmod +x start-services.sh
./start-services.sh
```

This will automatically:
1. Check Docker installation
2. Start all required services
3. Wait for health checks to pass
4. Display access information

## Manual Setup

### Step 1: Start Services

```bash
docker-compose up -d
```

### Step 2: Verify Services

```bash
docker-compose ps
```

All services should show `healthy` status.

### Step 3: Run Database Migrations

```bash
cd src/HookVerse.Api
dotnet ef database update
```

### Step 4: Start Application

Open 3 terminals:

**Terminal 1 - API:**
```bash
cd src/HookVerse.Api
dotnet run
```

**Terminal 2 - Worker:**
```bash
cd src/HookVerse.Worker
dotnet run
```

**Terminal 3 - Dashboard:**
```bash
cd src/HookVerse.Dashboard
dotnet run
```

## Access Points

| Service | URL/Connection | Credentials |
|---------|----------------|-------------|
| **API** | https://localhost:5000 | N/A |
| **API Swagger** | https://localhost:5000/swagger | N/A |
| **Dashboard** | https://localhost:5001 | N/A |
| **PostgreSQL** | localhost:5432 | hookverse/hookverse123 |
| **RabbitMQ AMQP** | localhost:5672 | guest/guest |
| **RabbitMQ Management** | http://localhost:15672 | guest/guest |
| **Redis** | localhost:6379 | password: hookverse123 |
| **pgAdmin** (optional) | http://localhost:5050 | admin@hookverse.local/admin123 |

## Environment Variables

Copy `.env.example` to `.env` and customize if needed:

```bash
cp .env.example .env
```

Default values work for local development without changes.

## Troubleshooting

### Port Already in Use

If you see port conflict errors:

```bash
# Stop existing containers
docker-compose down

# Start again
docker-compose up -d
```

### Service Not Healthy

Check logs for specific service:

```bash
docker-compose logs -f postgres
docker-compose logs -f rabbitmq
docker-compose logs -f redis
```

### Database Connection Failed

1. Verify PostgreSQL is running:
   ```bash
   docker-compose ps postgres
   ```

2. Check connection string in `appsettings.Development.json`:
   ```json
   "DefaultConnection": "Host=localhost;Port=5432;Database=hookverse;Username=hookverse;Password=hookverse123"
   ```

### RabbitMQ Connection Failed

1. Wait 30 seconds after startup for RabbitMQ to initialize
2. Check management UI: http://localhost:15672
3. Verify credentials: guest/guest

### Reset Everything

```bash
# Stop and remove all containers and volumes
docker-compose down -v

# Start fresh
docker-compose up -d

# Re-run migrations
cd src/HookVerse.Api
dotnet ef database update
```

## Development Workflow

### Daily Workflow

1. **Start services** (if not running):
   ```bash
   docker-compose up -d
   ```

2. **Start application** (3 terminals):
   ```bash
   # Terminal 1
   cd src/HookVerse.Api && dotnet run
   
   # Terminal 2
   cd src/HookVerse.Worker && dotnet run
   
   # Terminal 3
   cd src/HookVerse.Dashboard && dotnet run
   ```

3. **Make changes** and the apps will hot-reload

4. **Stop services** when done:
   ```bash
   docker-compose down
   ```

### Running Tests

```bash
# All tests
dotnet test

# Specific project
dotnet test tests/HookVerse.Core.Tests

# With coverage
dotnet test /p:CollectCoverage=true
```

### Database Migrations

**Create a new migration:**
```bash
cd src/HookVerse.Api
dotnet ef migrations add MigrationName
```

**Apply migrations:**
```bash
dotnet ef database update
```

**Rollback migration:**
```bash
dotnet ef database update PreviousMigrationName
```

## Docker Commands Reference

### Service Management

```bash
# Start all services
docker-compose up -d

# Start with pgAdmin
docker-compose --profile tools up -d

# Stop all services
docker-compose down

# Stop and remove volumes (clean slate)
docker-compose down -v

# Restart a service
docker-compose restart postgres

# View logs
docker-compose logs -f

# View logs for specific service
docker-compose logs -f rabbitmq
```

### Debugging

```bash
# Execute command in container
docker-compose exec postgres psql -U hookverse -d hookverse
docker-compose exec redis redis-cli -a hookverse123
docker-compose exec rabbitmq rabbitmqctl status

# View resource usage
docker stats

# Inspect container
docker-compose exec postgres sh
```

### Cleanup

```bash
# Remove stopped containers
docker-compose rm

# Remove all unused resources
docker system prune -a

# Remove volumes
docker volume prune
```

## Performance Tips

### Docker Desktop Settings

Recommended settings for optimal performance:

- **Memory**: 4 GB minimum, 8 GB recommended
- **CPUs**: 2 minimum, 4 recommended
- **Disk**: 50 GB recommended

### PostgreSQL

For development, the default settings are fine. For production-like testing:

```sql
-- Connect to database
\c hookverse

-- Check current connections
SELECT count(*) FROM pg_stat_activity;

-- View table sizes
SELECT 
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS size
FROM pg_tables
WHERE schemaname = 'public'
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;
```

## Additional Tools

### pgAdmin Setup

1. Start with pgAdmin:
   ```bash
   docker-compose --profile tools up -d
   ```

2. Access: http://localhost:5050
3. Login: admin@hookverse.local / admin123
4. Add server:
   - **Name**: HookVerse Local
   - **Host**: postgres (or host.docker.internal on Windows/Mac)
   - **Port**: 5432
   - **Database**: hookverse
   - **Username**: hookverse
   - **Password**: hookverse123

### RabbitMQ Management

Access: http://localhost:15672

Features:
- View queues and exchanges
- Monitor message rates
- Manage virtual hosts
- View connections and channels

### Redis CLI

```bash
# Connect to Redis
docker-compose exec redis redis-cli -a hookverse123

# Common commands
> PING
> INFO
> KEYS *
> GET key
> SET key value
```

## Next Steps

Once your environment is running:

1. ✅ All services healthy
2. ✅ Migrations applied
3. ✅ Applications started
4. 📖 Read the [API Documentation](http://localhost:5000/swagger)
5. 🧪 Run the test suite: `dotnet test`
6. 📊 Explore the Dashboard: http://localhost:5001
7. 🔍 Check RabbitMQ queues: http://localhost:15672

## Getting Help

- 📚 Full Docker documentation: [DOCKER.md](DOCKER.md)
- 📖 API documentation: http://localhost:5000/swagger
- 🐛 Issues: https://github.com/yourusername/HookVerse/issues
- 💬 Discussions: https://github.com/yourusername/HookVerse/discussions
