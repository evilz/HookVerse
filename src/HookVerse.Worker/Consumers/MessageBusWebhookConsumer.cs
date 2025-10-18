using HookVerse.Core.Interfaces;
using HookVerse.Shared.Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HookVerse.Worker.Consumers;

/// <summary>
/// Consumer for external message bus webhooks (RabbitMQ, Kafka, SQS)
/// Consumes messages from external queues and triggers webhook delivery
/// </summary>
public class MessageBusWebhookConsumer : IConsumer<ExternalWebhookMessage>
{
    private readonly IWebhookService _webhookService;
    private readonly IEventTypeRepository _eventTypeRepository;
    private readonly ILogger<MessageBusWebhookConsumer> _logger;

    public MessageBusWebhookConsumer(
        IWebhookService webhookService,
        IEventTypeRepository eventTypeRepository,
        ILogger<MessageBusWebhookConsumer> logger)
    {
        _webhookService = webhookService;
        _eventTypeRepository = eventTypeRepository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ExternalWebhookMessage> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Received external webhook message: EventType={EventType}, MessageId={MessageId}, Source={Source}, Subscriber={SubscriberId}",
            message.EventType, context.MessageId, message.Source, message.SubscriberId);

        try
        {
            // Resolve event type by name, version, and subscriber
            // Default to version "1.0" if not specified
            var version = message.Version ?? "1.0";
            var eventType = await _eventTypeRepository.GetByNameVersionSubscriberAsync(
                message.EventType,
                version,
                message.SubscriberId,
                context.CancellationToken);

            if (eventType == null)
            {
                _logger.LogWarning(
                    "Event type not found: EventType={EventType}, Version={Version}, SubscriberId={SubscriberId}. Message will be moved to DLQ.",
                    message.EventType, version, message.SubscriberId);
                
                // Don't retry - event type doesn't exist
                return;
            }

            // Convert metadata dictionary to JSON string if present
            string? metadataJson = null;
            if (message.Metadata != null && message.Metadata.Count > 0)
            {
                metadataJson = JsonSerializer.Serialize(message.Metadata);
            }

            // Map external message to internal format and send webhook
            var webhookEvent = await _webhookService.SendWebhookAsync(
                eventType.Id,
                message.SubscriberId,
                message.Payload,
                message.ScheduledFor,
                metadataJson,
                context.CancellationToken);

            // Acknowledge successful processing
            _logger.LogInformation(
                "Successfully processed external webhook message: WebhookId={WebhookId}, EventType={EventType}, Version={Version}, MessageId={MessageId}",
                webhookEvent.Id, message.EventType, version, context.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to process external webhook message: EventType={EventType}, MessageId={MessageId}, Error={Error}",
                message.EventType, context.MessageId, ex.Message);

            // Throw to trigger retry/dead-letter behavior
            throw;
        }
    }
}

/// <summary>
/// External webhook message format consumed from message bus
/// This is the contract for messages published by external systems
/// </summary>
public record ExternalWebhookMessage
{
    /// <summary>
    /// The subscriber ID that owns this webhook
    /// </summary>
    public Guid SubscriberId { get; init; }

    /// <summary>
    /// The event type name (e.g., "order.created", "payment.processed")
    /// </summary>
    public string EventType { get; init; } = string.Empty;

    /// <summary>
    /// The event type version (defaults to "1.0" if not specified)
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// The event payload (JSON string)
    /// </summary>
    public string Payload { get; init; } = string.Empty;

    /// <summary>
    /// Optional metadata key-value pairs
    /// </summary>
    public Dictionary<string, string>? Metadata { get; init; }

    /// <summary>
    /// Source system identifier
    /// </summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>
    /// Timestamp when the event was created
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Optional scheduled delivery time for the webhook
    /// </summary>
    public DateTime? ScheduledFor { get; init; }

    /// <summary>
    /// Optional trace/correlation ID for distributed tracing
    /// </summary>
    public string? TraceId { get; init; }
}
