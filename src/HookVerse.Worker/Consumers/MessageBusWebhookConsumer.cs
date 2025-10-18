using HookVerse.Core.Interfaces;
using HookVerse.Shared.Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace HookVerse.Worker.Consumers;

/// <summary>
/// Consumer for external message bus webhooks (RabbitMQ, Kafka, SQS)
/// Consumes messages from external queues and triggers webhook delivery
/// </summary>
public class MessageBusWebhookConsumer : IConsumer<ExternalWebhookMessage>
{
    private static readonly ActivitySource ActivitySource = new("HookVerse.MessageBus");
    
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
        var startTime = DateTime.UtcNow;

        // Start distributed tracing activity
        using var activity = ActivitySource.StartActivity("MessageBus.ConsumeWebhook", ActivityKind.Consumer);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.message_id", context.MessageId);
        activity?.SetTag("messaging.correlation_id", message.TraceId);
        activity?.SetTag("webhook.event_type", message.EventType);
        activity?.SetTag("webhook.subscriber_id", message.SubscriberId);
        activity?.SetTag("webhook.source", message.Source);

        _logger.LogInformation(
            "Received external webhook message: EventType={EventType}, MessageId={MessageId}, Source={Source}, Subscriber={SubscriberId}, TraceId={TraceId}",
            message.EventType, context.MessageId, message.Source, message.SubscriberId, message.TraceId);

        try
        {
            // Resolve event type by name, version, and subscriber
            // Default to version "1.0" if not specified
            var version = message.Version ?? "1.0";
            
            _logger.LogDebug(
                "Looking up event type: EventType={EventType}, Version={Version}, Subscriber={SubscriberId}",
                message.EventType, version, message.SubscriberId);

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
                
                activity?.SetStatus(ActivityStatusCode.Error, "Event type not found");
                activity?.SetTag("error", true);
                activity?.SetTag("error.type", "EventTypeNotFound");
                
                // Don't retry - event type doesn't exist
                return;
            }

            activity?.SetTag("webhook.event_type_id", eventType.Id);

            // Convert metadata dictionary to JSON string if present
            string? metadataJson = null;
            if (message.Metadata != null && message.Metadata.Count > 0)
            {
                metadataJson = JsonSerializer.Serialize(message.Metadata);
                _logger.LogDebug(
                    "Serialized metadata: MetadataCount={Count}, Subscriber={SubscriberId}",
                    message.Metadata.Count, message.SubscriberId);
            }

            // Map external message to internal format and send webhook
            _logger.LogInformation(
                "Sending webhook: EventTypeId={EventTypeId}, SubscriberId={SubscriberId}, ScheduledFor={ScheduledFor}",
                eventType.Id, message.SubscriberId, message.ScheduledFor);

            var webhookEvent = await _webhookService.SendWebhookAsync(
                eventType.Id,
                message.SubscriberId,
                message.Payload,
                message.ScheduledFor,
                metadataJson,
                context.CancellationToken);

            var processingTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

            activity?.SetTag("webhook.id", webhookEvent.Id);
            activity?.SetTag("webhook.processing_time_ms", processingTime);
            activity?.SetStatus(ActivityStatusCode.Ok);

            // Acknowledge successful processing
            _logger.LogInformation(
                "Successfully processed external webhook message: WebhookId={WebhookId}, EventType={EventType}, Version={Version}, MessageId={MessageId}, ProcessingTimeMs={ProcessingTimeMs}",
                webhookEvent.Id, message.EventType, version, context.MessageId, processingTime);
        }
        catch (Exception ex)
        {
            var processingTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
            
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error", true);
            activity?.SetTag("error.type", ex.GetType().Name);
            activity?.SetTag("error.message", ex.Message);
            activity?.SetTag("webhook.processing_time_ms", processingTime);

            _logger.LogError(ex,
                "Failed to process external webhook message: EventType={EventType}, MessageId={MessageId}, Error={Error}, ProcessingTimeMs={ProcessingTimeMs}",
                message.EventType, context.MessageId, ex.Message, processingTime);

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
