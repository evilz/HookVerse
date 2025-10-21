# ADR-001: .NET Technology Stack Selection

**Status**: Accepted  
**Date**: 2025-10-18  
**Deciders**: Platform Team  
**Tags**: technology, platform, language

## Context

HookVerse requires a high-performance, cloud-native platform for webhook delivery with:
- High throughput (10,000+ webhooks/second per instance)
- Low latency (< 500ms p95 for delivery)
- Cross-platform deployment (Linux containers, Kubernetes)
- Strong type safety and tooling
- Rich ecosystem for HTTP, messaging, and data access
- Open-source licensing

The technology stack decision is foundational and affects all future development.

## Decision

We will use **.NET 10 (C#)** as the primary technology stack for HookVerse, with the following key frameworks:

- **ASP.NET Core 10**: REST API services
- **Blazor Server**: Interactive dashboard UI
- **Entity Framework Core 10**: Data abstraction layer
- **MassTransit**: Message bus abstraction
- **OpenTelemetry .NET**: Distributed tracing and metrics
- **xUnit**: Unit and integration testing

## Consequences

### Positive

- **Performance**: .NET 10 delivers exceptional performance with minimal GC pressure
  - Benchmarks show 6.5M requests/second on basic endpoints
  - Native AOT compilation available for startup optimization
  - Excellent async/await support for I/O-bound operations

- **Developer Productivity**: 
  - Strong typing with C# prevents entire classes of runtime errors
  - Rich IDE support (Visual Studio, VS Code, JetBrains Rider)
  - Comprehensive standard library and NuGet ecosystem
  - LINQ provides expressive data manipulation

- **Cross-Platform**: 
  - Native Linux support with optimized containers
  - Runs on Windows, Linux, macOS without code changes
  - Minimal container images (< 100MB with Alpine)

- **Cloud-Native**: 
  - Built-in health checks, logging, configuration
  - Excellent Kubernetes support
  - OpenTelemetry native integration
  - Horizontal scaling with stateless architecture

- **Ecosystem**: 
  - MassTransit provides transport-agnostic messaging (RabbitMQ, Kafka, SQS)
  - EF Core supports PostgreSQL, SQL Server, MySQL, SQLite
  - Rich HTTP client libraries with Polly for resilience
  - Comprehensive testing frameworks

- **Open Source**: 
  - .NET is fully open source (MIT license)
  - Active community and Microsoft backing
  - Transparent development process on GitHub

### Negative

- **Container Size**: Larger than Go/Rust (though still reasonable with Alpine: ~100MB vs ~10-20MB)
- **Memory Footprint**: Higher baseline memory than native languages (~50-100MB vs ~10-20MB)
- **Learning Curve**: C# and .NET ecosystem has learning curve for developers unfamiliar with Microsoft stack
- **JIT Warm-up**: Cold start performance slower than AOT languages (mitigated in .NET 10 with dynamic PGO)

### Neutral

- **Platform Lock-in**: While .NET is cross-platform, the ecosystem is deeply tied to Microsoft's vision and roadmap
- **Runtime Dependency**: Requires .NET runtime (though can be self-contained or AOT compiled)

## Alternatives Considered

### Alternative 1: Go + Fiber/Echo

**Pros**:
- Smaller binaries and containers (10-20MB)
- Lower memory footprint (10-20MB baseline)
- Fast cold starts
- Native concurrency with goroutines
- Growing webhook platform ecosystem

**Cons**:
- Lack of generics until Go 1.18 (now available but ecosystem still catching up)
- Weaker type system (no sum types, limited type constraints)
- Less mature ORM options (GORM vs EF Core)
- Manual error handling can be verbose
- Smaller ecosystem compared to .NET

**Why rejected**: While Go's performance characteristics are excellent, .NET 10 now matches or exceeds Go in many benchmarks while providing superior developer experience, type safety, and ecosystem maturity. The memory and container size differences are acceptable trade-offs for a server-side application.

### Alternative 2: Node.js + TypeScript + Express

**Pros**:
- Large JavaScript/TypeScript ecosystem
- Excellent async I/O performance
- Familiar to many developers
- Rich NPM package ecosystem
- Good for real-time applications

**Cons**:
- Single-threaded event loop limits CPU-bound operations
- TypeScript provides weaker type safety than C# (structural typing, `any` escape hatch)
- Higher memory usage per request
- Less predictable performance characteristics
- Weak ORM ecosystem (TypeORM vs EF Core)

**Why rejected**: While Node.js excels at I/O-bound workloads, .NET provides better CPU-bound performance, stronger type safety, and more predictable scaling characteristics. The webhook delivery use case benefits from true multi-threading for parallel HTTP requests.

### Alternative 3: Java + Spring Boot

**Pros**:
- Mature enterprise ecosystem
- Excellent Spring framework
- Strong type system
- Comprehensive testing tools
- Large talent pool

**Cons**:
- Larger memory footprint (200-500MB baseline)
- Slower cold starts (JVM warm-up)
- More verbose syntax compared to C#
- Larger container images (200-500MB)
- Complex dependency management

**Why rejected**: While Java/Spring is battle-tested and mature, .NET 10 provides comparable ecosystem maturity with better performance, smaller footprint, and more modern language features (records, pattern matching, async/await). C# syntax is less verbose than Java.

### Alternative 4: Rust + Actix-web/Axum

**Pros**:
- Exceptional performance and safety
- Minimal memory footprint (10-20MB)
- Tiny container images (10-20MB)
- Zero-cost abstractions
- No garbage collection pauses

**Cons**:
- Steep learning curve (ownership, lifetimes)
- Smaller ecosystem for enterprise patterns
- Longer development time
- Limited ORM options (Diesel vs EF Core)
- Fewer developers with Rust expertise

**Why rejected**: While Rust provides superior low-level performance, the development velocity trade-off is significant. .NET 10's performance is "good enough" for webhook delivery (10K+/sec) while providing much faster development cycles and a larger talent pool. Rust would be reconsidered for ultra-low-latency edge workers if needed.

## References

- [.NET 10 Performance Improvements](https://devblogs.microsoft.com/dotnet/performance-improvements-in-net-10/)
- [TechEmpower Web Framework Benchmarks](https://www.techempower.com/benchmarks/)
- [ASP.NET Core Performance Best Practices](https://learn.microsoft.com/en-us/aspnet/core/performance/performance-best-practices)
- [Cloud Native .NET](https://learn.microsoft.com/en-us/dotnet/architecture/cloud-native/)
