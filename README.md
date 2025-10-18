# HookVerse

A high-performance, scalable webhook delivery platform built with .NET 10, designed to reliably deliver webhooks with advanced features like retries, circuit breakers, and schema validation.

## Features

- **Reliable Delivery**: Automatic retries with exponential backoff and circuit breakers
- **Schema Validation**: Support for JSON Schema, Avro, and Protocol Buffers
- **Multiple Transport Modes**: Kafka, RabbitMQ, and direct HTTP delivery
- **Real-time Monitoring**: Live dashboard with delivery statistics and health monitoring
- **High Performance**: Optimized for high-throughput webhook delivery
- **Multi-tenancy**: API key-based authentication with tenant isolation
- **Observability**: Built-in metrics, logging, and distributed tracing

## Architecture

- **HookVerse.Api**: REST API for webhook management
- **HookVerse.Worker**: Background service for webhook delivery
- **HookVerse.Dashboard**: Blazor Server UI for monitoring and management
- **HookVerse.Core**: Domain logic and business rules
- **HookVerse.Infrastructure**: Data access and external integrations
- **HookVerse.Shared**: Shared contracts and utilities

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker](https://www.docker.com/) (for running dependencies)
- [PostgreSQL 16+](https://www.postgresql.org/) (or use Docker)
- [RabbitMQ 3.13+](https://www.rabbitmq.com/) (or use Docker)
- [Redis 7+](https://redis.io/) (optional, for caching)

## Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/yourusername/HookVerse.git
cd HookVerse
```

### 2. Start dependencies with Docker Compose

```bash
docker-compose up -d
```

This will start:
- **PostgreSQL 16** on port `5432`
- **RabbitMQ 3.13** on ports `5672` (AMQP) and `15672` (Management UI)
- **Redis 7** on port `6379`

Access RabbitMQ Management UI at `http://localhost:15672` (guest/guest)

> **Note**: To include pgAdmin for database management, run: `docker-compose --profile tools up -d`
> Access pgAdmin at `http://localhost:5050` (admin@hookverse.local/admin123)

For detailed Docker setup instructions, see [DOCKER.md](DOCKER.md)

### 3. Verify services are running

```bash
docker-compose ps
```

All services should show "healthy" status.

### 4. Run database migrations

```bash
cd src/HookVerse.Api
dotnet ef database update
```

### 5. Run the application

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

The services will be available at:
- API: `https://localhost:5000` (Swagger: `https://localhost:5000/swagger`)
- Dashboard: `https://localhost:5001`

## Development

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Run specific test project
dotnet test tests/HookVerse.Core.Tests
```

### Code Style

This project follows standard C# coding conventions. An `.editorconfig` file is included to enforce consistent formatting.

### Project Structure

```
HookVerse/
├── src/
│   ├── HookVerse.Api/          # REST API
│   ├── HookVerse.Core/         # Domain logic
│   ├── HookVerse.Infrastructure/ # Data access & external deps
│   ├── HookVerse.Worker/       # Background webhook delivery
│   ├── HookVerse.Dashboard/    # Blazor monitoring UI
│   └── HookVerse.Shared/       # Shared contracts
├── tests/
│   ├── HookVerse.Api.Tests/
│   ├── HookVerse.Core.Tests/
│   ├── HookVerse.Integration.Tests/
│   └── HookVerse.Contract.Tests/
├── docker/
│   ├── docker-compose.yml
│   ├── Dockerfile.api
│   ├── Dockerfile.worker
│   └── Dockerfile.dashboard
└── docs/
    └── architecture/           # Architecture Decision Records
```

## Deployment

### Docker Compose (All Services)

```bash
# Build images
docker-compose build

# Run all services
docker-compose up -d
```

### Kubernetes

_(Kubernetes deployment manifests coming soon)_

## API Documentation

Once the API is running, visit `https://localhost:5000/swagger` for interactive API documentation.

## Monitoring

The Dashboard provides real-time monitoring of:
- Webhook delivery success/failure rates
- Retry statistics
- Circuit breaker status
- System health metrics

Access the dashboard at `https://localhost:5001`

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For issues and questions:
- GitHub Issues: https://github.com/yourusername/HookVerse/issues
- Documentation: https://github.com/yourusername/HookVerse/wiki

## Roadmap

- [ ] Kafka transport support
- [ ] Advanced filtering and transformation rules
- [ ] Webhook payload signing and verification
- [ ] Multi-region deployment support
- [ ] GraphQL API
- [ ] Terraform deployment templates
