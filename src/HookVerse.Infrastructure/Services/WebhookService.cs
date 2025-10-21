using HookVerse.Core.Entities;
using HookVerse.Core.Enums;
using HookVerse.Core.Interfaces;
using HookVerse.Infrastructure.Metrics;
using HookVerse.Infrastructure.SchemaValidation;
using HookVerse.Shared.Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace HookVerse.Infrastructure.Services;

/// <summary>
/// Service for webhook operations including validation and publishing.
/// </summary>
public class WebhookService : IWebhookService
{
    private const int MaxPayloadSizeBytes = 1048576; // 1MB
    
    private readonly IWebhookEventRepository _webhookEventRepository;
    private readonly IEventTypeRepository _eventTypeRepository;
    private readonly ISubscriberRepository _subscriberRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly SchemaValidatorFactory _validatorFactory;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<WebhookService> _logger;
    private readonly SchemaValidationMetrics? _metrics;

    public WebhookService(
        IWebhookEventRepository webhookEventRepository,
        IEventTypeRepository eventTypeRepository,
        ISubscriberRepository subscriberRepository,
        ISubscriptionRepository subscriptionRepository,
        SchemaValidatorFactory validatorFactory,
        IPublishEndpoint publishEndpoint,
        ILogger<WebhookService> logger,
        SchemaValidationMetrics? metrics = null)
    {
        _webhookEventRepository = webhookEventRepository;
        _eventTypeRepository = eventTypeRepository;
        _subscriberRepository = subscriberRepository;
        _subscriptionRepository = subscriptionRepository;
        _validatorFactory = validatorFactory;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task<WebhookEvent> SendWebhookAsync(
        Guid eventTypeId, 
        Guid subscriberId, 
        string payload, 
        DateTime? scheduledFor = null, 
        string? metadata = null, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending webhook for EventType {EventTypeId} from Subscriber {SubscriberId}", eventTypeId, subscriberId);

        // Validate event type exists and is active
        var eventType = await _eventTypeRepository.GetByIdAsync(eventTypeId, cancellationToken);
        if (eventType == null)
        {
            throw new InvalidOperationException($"EventType {eventTypeId} not found");
        }

        if (!eventType.IsActive)
        {
            throw new InvalidOperationException($"EventType {eventType.Name} is not active");
        }

        // Validate subscriber
        var subscriber = await _subscriberRepository.GetByIdAsync(subscriberId, cancellationToken);
        if (subscriber == null)
        {
            throw new InvalidOperationException($"Subscriber {subscriberId} not found");
        }

        if (!subscriber.IsActive)
        {
            throw new InvalidOperationException($"Subscriber {subscriber.Name} is not active");
        }

        // Validate payload size
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        if (payloadBytes.Length > MaxPayloadSizeBytes)
        {
            throw new InvalidOperationException($"Payload exceeds maximum size of {MaxPayloadSizeBytes} bytes");
        }

        // Validate payload is valid JSON
        try
        {
            JsonDocument.Parse(payload);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Payload is not valid JSON", ex);
        }

        // Validate against schema if present and active
        if (eventType.SchemaDefinition != null && eventType.SchemaDefinition.IsActive)
        {
            var stopwatch = Stopwatch.StartNew();
            
            _logger.LogDebug(
                "Validating payload against schema {SchemaId} (format: {Format})",
                eventType.SchemaDefinition.Id,
                eventType.SchemaDefinition.Format);

            try
            {
                var schemaType = MapSchemaFormatToType(eventType.SchemaDefinition.Format);
                var validator = _validatorFactory.GetValidator(schemaType);
                var validationResult = await validator.ValidateAsync(payload, eventType.SchemaDefinition.Content, cancellationToken);
                
                stopwatch.Stop();
                
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning(
                        "Schema validation failed for EventType {EventTypeId}: {Errors}",
                        eventTypeId,
                        string.Join(", ", validationResult.Errors));

                    // Record metrics for failure
                    _metrics?.RecordValidationFailure(
                        eventTypeId, 
                        eventType.SchemaDefinition.Format,
                        validationResult.Errors.FirstOrDefault() ?? "Unknown");
                    _metrics?.RecordValidationDuration(
                        stopwatch.Elapsed.TotalMilliseconds,
                        eventType.SchemaDefinition.Format,
                        success: false);

                    throw new InvalidOperationException($"Payload does not match event type schema: {string.Join(", ", validationResult.Errors)}");
                }

                _logger.LogInformation(
                    "Schema validation succeeded for EventType {EventTypeId} in {Duration}ms",
                    eventTypeId,
                    stopwatch.Elapsed.TotalMilliseconds);

                // Record metrics for success
                _metrics?.RecordValidationSuccess(eventTypeId, eventType.SchemaDefinition.Format);
                _metrics?.RecordValidationDuration(
                    stopwatch.Elapsed.TotalMilliseconds,
                    eventType.SchemaDefinition.Format,
                    success: true);
            }
            catch (NotSupportedException ex)
            {
                stopwatch.Stop();
                
                _logger.LogError(ex, "Unsupported schema format for EventType {EventTypeId}", eventTypeId);
                
                _metrics?.RecordValidationFailure(
                    eventTypeId,
                    eventType.SchemaDefinition.Format,
                    "UnsupportedFormat");
                
                throw;
            }
        }
        else if (eventType.SchemaDefinition == null)
        {
            _logger.LogDebug("No schema defined for EventType {EventTypeId}, skipping validation", eventTypeId);
        }
        else
        {
            _logger.LogDebug("Schema for EventType {EventTypeId} is inactive, skipping validation", eventTypeId);
        }

        // Generate trace ID
        var traceId = Guid.NewGuid().ToString("N");

        // Compute payload hash
        var payloadHash = ComputeSha256Hash(payload);

        // Calculate expiry
        var now = DateTime.UtcNow;
        var expiresAt = now.AddDays(subscriber.RetentionDays);

        // Create webhook event
        var webhookEvent = new WebhookEvent
        {
            Id = Guid.NewGuid(),
            EventTypeId = eventTypeId,
            SubscriberId = subscriberId,
            Payload = payload, // TODO: Encrypt at rest
            PayloadHash = payloadHash,
            PayloadSizeBytes = payloadBytes.Length,
            TraceId = traceId,
            ScheduledFor = scheduledFor,
            ExpiresAt = expiresAt,
            Metadata = metadata,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _webhookEventRepository.AddAsync(webhookEvent, cancellationToken);
        await _webhookEventRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created WebhookEvent {WebhookEventId} with TraceId {TraceId}", webhookEvent.Id, traceId);

        // Get active subscriptions for this event type
        var subscriptions = await _subscriptionRepository.GetActiveByEventTypeIdAsync(eventTypeId, cancellationToken);

        // Publish delivery requests to message bus
        foreach (var subscription in subscriptions)
        {
            var message = new WebhookDeliveryRequested
            {
                WebhookId = webhookEvent.Id,
                SubscriptionId = subscription.Id,
                TenantId = subscriberId,
                Url = subscription.EndpointUrl,
                HttpMethod = "POST",
                Headers = new Dictionary<string, string>(),
                Payload = payload,
                RetryCount = 0,
                RequestedAt = now
            };

            await _publishEndpoint.Publish(message, cancellationToken);
            
            _logger.LogInformation("Published delivery request for Subscription {SubscriptionId}", subscription.Id);
        }

        return webhookEvent;
    }

    public async Task<WebhookEvent?> GetWebhookStatusAsync(Guid webhookEventId, CancellationToken cancellationToken = default)
    {
        return await _webhookEventRepository.GetWithDeliveryAttemptsAsync(webhookEventId, cancellationToken);
    }

    public async Task<bool> ValidatePayloadAsync(Guid eventTypeId, string payload, CancellationToken cancellationToken = default)
    {
        var eventType = await _eventTypeRepository.GetByIdAsync(eventTypeId, cancellationToken);
        if (eventType?.SchemaDefinition == null || !eventType.SchemaDefinition.IsActive)
        {
            // No schema to validate against or schema is inactive
            return true;
        }

        var schemaType = MapSchemaFormatToType(eventType.SchemaDefinition.Format);
        var validator = _validatorFactory.GetValidator(schemaType);
        var validationResult = await validator.ValidateAsync(payload, eventType.SchemaDefinition.Content, cancellationToken);
        return validationResult.IsValid;
    }

    private static string MapSchemaFormatToType(SchemaFormat format)
    {
        return format switch
        {
            SchemaFormat.JsonSchema => "json-schema",
            SchemaFormat.Avro => "avro",
            SchemaFormat.Protobuf => "protobuf",
            SchemaFormat.DotNetAssembly => "dotnet-assembly",
            _ => throw new NotSupportedException($"Schema format {format} is not supported")
        };
    }

    private static string ComputeSha256Hash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
