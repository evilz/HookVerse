using HookVerse.Core.Entities;
using HookVerse.Core.Interfaces;
using HookVerse.Shared.Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;
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
    private readonly ISchemaValidator _schemaValidator;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(
        IWebhookEventRepository webhookEventRepository,
        IEventTypeRepository eventTypeRepository,
        ISubscriberRepository subscriberRepository,
        ISubscriptionRepository subscriptionRepository,
        ISchemaValidator schemaValidator,
        IPublishEndpoint publishEndpoint,
        ILogger<WebhookService> logger)
    {
        _webhookEventRepository = webhookEventRepository;
        _eventTypeRepository = eventTypeRepository;
        _subscriberRepository = subscriberRepository;
        _subscriptionRepository = subscriptionRepository;
        _schemaValidator = schemaValidator;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
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

        // Validate against schema if present
        if (eventType.SchemaDefinition != null)
        {
            var validationResult = await _schemaValidator.ValidateAsync(payload, eventType.SchemaDefinition.Content, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new InvalidOperationException($"Payload does not match event type schema: {string.Join(", ", validationResult.Errors)}");
            }
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
        if (eventType?.SchemaDefinition == null)
        {
            // No schema to validate against
            return true;
        }

        var validationResult = await _schemaValidator.ValidateAsync(payload, eventType.SchemaDefinition.Content, cancellationToken);
        return validationResult.IsValid;
    }

    private static string ComputeSha256Hash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
