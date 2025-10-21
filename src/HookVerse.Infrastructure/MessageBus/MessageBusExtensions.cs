using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HookVerse.Infrastructure.MessageBus;

/// <summary>
/// Extension methods for configuring MassTransit message bus
/// </summary>
public static class MessageBusExtensions
{
    /// <summary>
    /// Add MassTransit with RabbitMQ configuration
    /// </summary>
    public static IServiceCollection AddMessageBus(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            // Register consumers (will be added as they are created)
            // x.AddConsumer<WebhookDeliveryConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbitMqConfig = configuration.GetSection("RabbitMQ");
                var host = rabbitMqConfig["Host"] ?? "localhost";
                var portStr = rabbitMqConfig["Port"];
                var port = int.TryParse(portStr, out var p) ? p : 5672;
                var username = rabbitMqConfig["Username"] ?? "guest";
                var password = rabbitMqConfig["Password"] ?? "guest";
                var virtualHost = rabbitMqConfig["VirtualHost"] ?? "/";

                cfg.Host(host, (ushort)port, virtualHost, h =>
                {
                    h.Username(username);
                    h.Password(password);
                });

                // Configure retry policy
                cfg.UseMessageRetry(r => r.Intervals(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(10)
                ));

                // Configure endpoints
                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }

    /// <summary>
    /// Add MassTransit with Kafka configuration (alternative transport)
    /// </summary>
    public static IServiceCollection AddKafkaMessageBus(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            x.UsingInMemory((context, cfg) =>
            {
                cfg.ConfigureEndpoints(context);
            });

            // Kafka rider configuration
            x.AddRider(rider =>
            {
                // Register consumers
                // rider.AddConsumer<WebhookDeliveryConsumer>();

                rider.UsingKafka((context, k) =>
                {
                    var kafkaConfig = configuration.GetSection("Kafka");
                    var bootstrapServers = kafkaConfig["BootstrapServers"] ?? "localhost:9092";

                    k.Host(bootstrapServers);

                    // Configure topics
                    k.TopicEndpoint<object>("webhook-delivery", "webhook-consumer-group", e =>
                    {
                        // Configure topic-specific settings
                        e.AutoOffsetReset = Confluent.Kafka.AutoOffsetReset.Earliest;
                    });
                });
            });
        });

        return services;
    }
}
