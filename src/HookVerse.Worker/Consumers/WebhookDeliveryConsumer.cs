using HookVerse.Core.Enums;
using HookVerse.Core.Interfaces;
using HookVerse.Shared.Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;

namespace HookVerse.Worker.Consumers;

/// <summary>
/// Consumer for webhook delivery requests with retry logic and circuit breaker.
/// </summary>
public class WebhookDeliveryConsumer : IConsumer<WebhookDeliveryRequested>
{
    private const int MaxRetryAttempts = 5;
    
    private readonly IWebhookEventRepository _webhookEventRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IDeliveryService _deliveryService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<WebhookDeliveryConsumer> _logger;

    // Retry policy: 1s, 5s, 25s, 2m, 10m exponential backoff
    private static readonly TimeSpan[] RetryDelays = new[]
    {
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(25),
        TimeSpan.FromMinutes(2),
        TimeSpan.FromMinutes(10)
    };

    public WebhookDeliveryConsumer(
        IWebhookEventRepository webhookEventRepository,
        ISubscriptionRepository subscriptionRepository,
        IDeliveryService deliveryService,
        IPublishEndpoint publishEndpoint,
        ILogger<WebhookDeliveryConsumer> logger)
    {
        _webhookEventRepository = webhookEventRepository;
        _subscriptionRepository = subscriptionRepository;
        _deliveryService = deliveryService;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<WebhookDeliveryRequested> context)
    {
        var message = context.Message;
        
        _logger.LogInformation(
            "Processing webhook delivery: WebhookId={WebhookId}, SubscriptionId={SubscriptionId}, RetryCount={RetryCount}",
            message.WebhookId, message.SubscriptionId, message.RetryCount);

        try
        {
            // Load webhook event
            var webhookEvent = await _webhookEventRepository.GetByIdAsync(message.WebhookId);
            if (webhookEvent == null)
            {
                _logger.LogWarning("Webhook event {WebhookId} not found", message.WebhookId);
                return;
            }

            // Check if expired
            if (webhookEvent.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogInformation("Webhook event {WebhookId} has expired, skipping delivery", message.WebhookId);
                return;
            }

            // Load subscription
            var subscription = await _subscriptionRepository.GetByIdAsync(message.SubscriptionId);
            if (subscription == null)
            {
                _logger.LogWarning("Subscription {SubscriptionId} not found", message.SubscriptionId);
                return;
            }

            // Check if subscription is active
            if (!subscription.IsActive)
            {
                _logger.LogInformation("Subscription {SubscriptionId} is inactive, skipping delivery", message.SubscriptionId);
                return;
            }

            // Calculate attempt number
            var attemptNumber = message.RetryCount + 1;

            // Attempt delivery
            var attempt = await _deliveryService.DeliverWebhookAsync(
                webhookEvent, 
                subscription, 
                attemptNumber, 
                context.CancellationToken);

            // Handle delivery result
            if (attempt.Status == DeliveryStatus.Delivered)
            {
                // Success - publish success message
                await _publishEndpoint.Publish(new WebhookDeliverySucceeded
                {
                    WebhookId = message.WebhookId,
                    DeliveryId = attempt.Id,
                    StatusCode = attempt.ResponseStatus ?? 200,
                    ResponseBody = attempt.ResponseBody ?? string.Empty,
                    Duration = TimeSpan.FromMilliseconds(attempt.DurationMs ?? 0),
                    CompletedAt = attempt.CompletedAt ?? DateTime.UtcNow
                }, context.CancellationToken);

                // Update subscription last delivery time
                await _subscriptionRepository.UpdateLastDeliveryAsync(
                    subscription.Id, 
                    DateTime.UtcNow, 
                    context.CancellationToken);

                _logger.LogInformation(
                    "Webhook {WebhookId} delivered successfully to subscription {SubscriptionId} on attempt {AttemptNumber}",
                    message.WebhookId, message.SubscriptionId, attemptNumber);
            }
            else if (attemptNumber < MaxRetryAttempts && 
                     attempt.Status != DeliveryStatus.Rejected && 
                     attempt.Status != DeliveryStatus.CircuitOpen)
            {
                // Schedule retry
                var retryDelay = RetryDelays[Math.Min(attemptNumber, RetryDelays.Length - 1)];
                var nextRetryAt = DateTime.UtcNow.Add(retryDelay);

                // Update attempt with retry schedule
                attempt.NextRetryAt = nextRetryAt;
                await _webhookEventRepository.SaveChangesAsync(context.CancellationToken);

                // Publish retry message
                await _publishEndpoint.Publish(new WebhookRetryScheduled
                {
                    WebhookId = message.WebhookId,
                    DeliveryId = attempt.Id,
                    RetryAttempt = attemptNumber + 1,
                    ScheduledFor = nextRetryAt,
                    RetryDelay = retryDelay
                }, context.CancellationToken);

                _logger.LogInformation(
                    "Webhook {WebhookId} delivery failed, scheduled retry #{RetryCount} at {NextRetryAt}",
                    message.WebhookId, attemptNumber + 1, nextRetryAt);

                // Re-publish with delay for retry
                await context.SchedulePublish(
                    nextRetryAt,
                    new WebhookDeliveryRequested
                    {
                        WebhookId = message.WebhookId,
                        SubscriptionId = message.SubscriptionId,
                        TenantId = message.TenantId,
                        Url = message.Url,
                        HttpMethod = message.HttpMethod,
                        Headers = message.Headers,
                        Payload = message.Payload,
                        RetryCount = attemptNumber,
                        RequestedAt = DateTime.UtcNow
                    });
            }
            else
            {
                // Dead letter - exhausted retries or permanently failed
                attempt.Status = DeliveryStatus.DeadLetter;
                await _webhookEventRepository.SaveChangesAsync(context.CancellationToken);

                // Publish failure message
                await _publishEndpoint.Publish(new WebhookDeliveryFailed
                {
                    WebhookId = message.WebhookId,
                    DeliveryId = attempt.Id,
                    ErrorMessage = attempt.ErrorMessage ?? "Unknown error",
                    StatusCode = attempt.ResponseStatus,
                    RetryCount = attemptNumber,
                    WillRetry = false,
                    NextRetryAt = null,
                    FailedAt = DateTime.UtcNow
                }, context.CancellationToken);

                _logger.LogError(
                    "Webhook {WebhookId} delivery permanently failed after {AttemptCount} attempts. Status: {Status}, Error: {Error}",
                    message.WebhookId, attemptNumber, attempt.Status, attempt.ErrorMessage);
            }
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogWarning(ex, 
                "Circuit breaker open for webhook {WebhookId}, will retry later", 
                message.WebhookId);

            // Re-throw to let MassTransit handle retry
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Unexpected error processing webhook delivery: WebhookId={WebhookId}", 
                message.WebhookId);

            // Re-throw to let MassTransit handle retry
            throw;
        }
    }
}
