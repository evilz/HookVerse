# Docker Setup for HookVerse

This directory contains Docker Compose configuration for running HookVerse dependencies locally.

## Services

The Docker Compose stack includes:

- **PostgreSQL 16** - Primary database (Port: 5432)
- **RabbitMQ 3.13** - Message bus with management UI (Ports: 5672, 15672)
- **Redis 7** - Caching and distributed locking (Port: 6379)
- **pgAdmin 4** - PostgreSQL management UI (Port: 5050) - Optional

## Quick Start

### 1. Start all services

```bash
docker-compose up -d
```

### 2. Start with pgAdmin (optional)

```bash
docker-compose --profile tools up -d
```

### 3. Check service status

```bash
docker-compose ps
```

### 4. View logs

```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f postgres
docker-compose logs -f rabbitmq
docker-compose logs -f redis
```

### 5. Stop services

```bash
docker-compose down
```

### 6. Stop and remove volumes (clean slate)

```bash
docker-compose down -v
```

## Service Access

### PostgreSQL
- **Host**: localhost
- **Port**: 5432
- **Database**: hookverse
- **Username**: hookverse
- **Password**: hookverse123
- **Connection String**: `Host=localhost;Port=5432;Database=hookverse;Username=hookverse;Password=hookverse123`

### RabbitMQ
- **AMQP Port**: 5672
- **Management UI**: http://localhost:15672
- **Username**: guest
- **Password**: guest
- **Connection**: `amqp://guest:guest@localhost:5672/`

### Redis
- **Host**: localhost
- **Port**: 6379
- **Password**: hookverse123
- **Connection String**: `localhost:6379,password=hookverse123`

### pgAdmin (Optional)
- **URL**: http://localhost:5050
- **Email**: admin@hookverse.local
- **Password**: admin123

#### Adding PostgreSQL Server in pgAdmin:
1. Right-click "Servers" → "Register" → "Server"
2. **General Tab**:
   - Name: HookVerse Local
3. **Connection Tab**:
   - Host: postgres (or host.docker.internal for Windows/Mac)
   - Port: 5432
   - Database: hookverse
   - Username: hookverse
   - Password: hookverse123

## Health Checks

All services include health checks. Check status:

```bash
docker-compose ps
```

Services should show "healthy" status after startup.

## Data Persistence

Data is persisted in Docker volumes:
- `postgres_data` - PostgreSQL database files
- `rabbitmq_data` - RabbitMQ data and configuration
- `redis_data` - Redis persistence files
- `pgadmin_data` - pgAdmin settings and configurations

## Troubleshooting

### Port conflicts

If ports are already in use, you can change them in `docker-compose.yml`:

```yaml
services:
  postgres:
    ports:
      - "15432:5432"  # Change left side to available port
```

### Reset everything

```bash
docker-compose down -v
docker-compose up -d
```

### Database not accessible

Check if PostgreSQL is healthy:

```bash
docker-compose ps postgres
docker-compose logs postgres
```

### RabbitMQ management UI not loading

Wait 30 seconds after startup, then check:

```bash
docker-compose logs rabbitmq
```

### Connect from containers

When services need to connect to each other within Docker network, use service names:
- PostgreSQL: `postgres:5432`
- RabbitMQ: `rabbitmq:5672`
- Redis: `redis:6379`

## Running Migrations

After starting PostgreSQL, run migrations from the project root:

```bash
cd src/HookVerse.Api
dotnet ef database update
```

Or using the migration script:

```bash
# Windows
.\scripts\migrate.ps1

# Linux/Mac
./scripts/migrate.sh
```

## Development Workflow

1. Start Docker services: `docker-compose up -d`
2. Wait for health checks to pass: `docker-compose ps`
3. Run migrations: `dotnet ef database update`
4. Start API: `dotnet run --project src/HookVerse.Api`
5. Start Worker: `dotnet run --project src/HookVerse.Worker`
6. Start Dashboard: `dotnet run --project src/HookVerse.Dashboard`

## Production Considerations

This Docker Compose setup is for **local development only**. For production:

1. Use separate hosted services (AWS RDS, CloudAMQP, Redis Cloud, etc.)
2. Enable SSL/TLS connections
3. Use strong passwords and secrets management
4. Configure proper backup strategies
5. Set up monitoring and alerting
6. Use production-grade resource limits
7. Implement proper network security

## Useful Commands

```bash
# Restart a single service
docker-compose restart rabbitmq

# View resource usage
docker stats

# Execute commands in containers
docker-compose exec postgres psql -U hookverse
docker-compose exec redis redis-cli -a hookverse123
docker-compose exec rabbitmq rabbitmqctl status

# Update images
docker-compose pull
docker-compose up -d

# Clean up unused resources
docker system prune -a
```
