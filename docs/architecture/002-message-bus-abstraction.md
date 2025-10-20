# ADR-002: Message Bus Abstraction with MassTransit

**Status**: Accepted  
**Date**: 2025-10-18  
**Deciders**: Platform Team  
**Tags**: architecture, messaging, integration

## Context

HookVerse needs to decouple webhook submission from webhook delivery to achieve:
- **Asynchronous processing**: API responds immediately, delivery happens in background
- **Horizontal scaling**: Multiple worker instances process webhooks in parallel
- **Reliability**: At-least-once delivery with retry and dead-letter handling
- **Flexibility**: Support multiple message broker backends (RabbitMQ, Kafka, AWS SQS)

The system must avoid vendor lock-in to a specific message broker while maintaining high performance and reliability.

## Decision

We will use **MassTransit** as the message bus abstraction layer with pluggable transports:

- **Default**: RabbitMQ (for local development and self-hosted deployments)
- **Alternative 1**: Apache Kafka (for high-throughput, event-streaming scenarios)
- **Alternative 2**: AWS SQS (for AWS-native deployments)
- **Alternative 3**: Azure Service Bus (for Azure-native deployments)

MassTransit configuration will be environment-driven via `MessageBus__Transport` setting.

## Consequences

### Positive

- **Transport Agnostic**: Switch between RabbitMQ, Kafka, SQS, Azure Service Bus without code changes
  ```csharp
  var transport = config["MessageBus:Transport"];
  if (transport == "RabbitMQ")
      configurator.UsingRabbitMq(...);
  else if (transport == "Kafka")
      configurator.UsingAzureServiceBus(...);
  ```

- **Built-in Patterns**: MassTransit provides tested implementations of:
  - Request/Response (webhook submission → acknowledgment)
  - Publish/Subscribe (event-driven architecture)
  - Saga (long-running workflows)
  - Retry policies (exponential backoff)
  - Circuit breakers
  - Dead-letter queues

- **Developer Experience**:
  - Strongly-typed message contracts (C# classes)
  - Automatic serialization (System.Text.Json)
  - Dependency injection integration
  - Comprehensive logging and diagnostics

- **Observability**: Native OpenTelemetry integration with automatic trace propagation

- **Reliability**: 
  - Automatic message acknowledgment after successful processing
  - Configurable retry policies with exponential backoff
  - Dead-letter queue for failed messages
  - Transactional outbox pattern support

- **Performance**: 
  - Efficient message batching
  - Concurrent consumer support
  - Prefetch optimization
  - Minimal serialization overhead

### Negative

- **Abstraction Overhead**: Small performance penalty vs direct broker clients (typically < 5%)
- **Learning Curve**: MassTransit has its own concepts (endpoints, consumers, sagas) that developers must learn
- **Configuration Complexity**: Multi-transport support requires careful configuration management
- **Dependency Weight**: MassTransit adds significant dependency footprint (~2-3MB)

### Neutral

- **Broker-Specific Features**: Advanced broker-specific features (e.g., Kafka stream processing) not accessible through abstraction
- **Version Lock**: MassTransit version updates may require code changes

## Alternatives Considered

### Alternative 1: Direct Broker Clients (RabbitMQ.Client, Confluent.Kafka)

**Pros**:
- Maximum performance (no abstraction overhead)
- Full access to broker-specific features
- Smaller dependency footprint
- Complete control over message handling

**Cons**:
- Manual implementation of retry, dead-letter, circuit breaker patterns
- Code changes required to switch brokers
- More boilerplate code for common patterns
- Manual integration with DI and OpenTelemetry
- Higher development and maintenance cost

**Why rejected**: The abstraction trade-off is worthwhile. MassTransit's reliability patterns, transport flexibility, and observability integration outweigh the small performance penalty. Manual implementation of these patterns is error-prone and time-consuming.

### Alternative 2: NServiceBus

**Pros**:
- Mature enterprise service bus framework
- Comprehensive reliability patterns
- Strong consistency guarantees with sagas
- Professional support available

**Cons**:
- **Commercial license** required for production use ($1,495/year per endpoint)
- Opinionated architecture may limit flexibility
- Heavier abstraction layer
- Smaller community vs MassTransit

**Why rejected**: The commercial licensing is incompatible with HookVerse's open-source nature. MassTransit provides comparable features with MIT licensing and a more active open-source community.

### Alternative 3: Azure Service Bus SDK Directly

**Pros**:
- Native Azure integration
- Excellent reliability and scaling
- Built-in dead-letter queues
- Managed service (no infrastructure)

**Cons**:
- **Azure lock-in**: Cannot run on-premises or other clouds without code changes
- Higher cost for high-throughput scenarios
- Limited to Azure ecosystem
- Requires separate implementation for other brokers

**Why rejected**: Cloud vendor lock-in violates the open-source, multi-cloud principle. MassTransit allows Azure Service Bus as one transport option without forcing it.

### Alternative 4: Kafka Directly (Confluent.Kafka)

**Pros**:
- Exceptional throughput (millions of messages/second)
- Event streaming capabilities
- Strong durability guarantees
- Industry-standard for event-driven architectures

**Cons**:
- Complex operational requirements (ZooKeeper/KRaft, partition management)
- Overkill for simple request/response patterns
- Steeper learning curve
- Higher infrastructure cost
- No built-in request/response pattern

**Why rejected**: Kafka's complexity is unnecessary for HookVerse's use case. Most deployments need simple pub/sub with request/response, not event streaming. MassTransit allows Kafka as an option for users who need it without forcing it on everyone.

## Implementation Guidelines

### Transport Selection

```csharp
// appsettings.json
{
  "MessageBus": {
    "Transport": "RabbitMQ",  // or "Kafka", "AzureServiceBus", "AmazonSQS"
    "Host": "localhost",
    "Port": 5672,
    "Username": "guest",
    "Password": "guest"
  }
}
```

### Consumer Example

```csharp
public class WebhookDeliveryConsumer : IConsumer<WebhookDeliveryMessage>
{
    private readonly IDeliveryService _deliveryService;
    
    public async Task Consume(ConsumeContext<WebhookDeliveryMessage> context)
    {
        var message = context.Message;
        await _deliveryService.DeliverWebhookAsync(
            message.WebhookEventId, 
            context.CancellationToken);
    }
}
```

### Configuration

```csharp
services.AddMassTransit(x =>
{
    x.AddConsumer<WebhookDeliveryConsumer>();
    
    var transport = configuration["MessageBus:Transport"];
    
    if (transport == "RabbitMQ")
    {
        x.UsingRabbitMq((context, cfg) =>
        {
            cfg.Host(configuration["MessageBus:Host"], h =>
            {
                h.Username(configuration["MessageBus:Username"]);
                h.Password(configuration["MessageBus:Password"]);
            });
            
            cfg.ConfigureEndpoints(context);
        });
    }
    else if (transport == "Kafka")
    {
        x.UsingKafka((context, cfg) =>
        {
            cfg.Host(configuration["MessageBus:Host"]);
            cfg.ConfigureEndpoints(context);
        });
    }
});
```

## References

- [MassTransit Documentation](https://masstransit.io/)
- [MassTransit Transport Comparison](https://masstransit.io/documentation/transports)
- [Message Broker Patterns](https://www.enterpriseintegrationpatterns.com/)
- [At-Least-Once Delivery](https://exactly-once.github.io/posts/at-least-once-delivery/)
