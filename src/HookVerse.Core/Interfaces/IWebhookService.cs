using HookVerse.Core.Entities;

namespace HookVerse.Core.Interfaces;

/// <summary>
/// Service interface for webhook operations.
/// </summary>
public interface IWebhookService
{
    /// <summary>
    /// Sends a webhook by creating an event and publishing to the message bus.
    /// </summary>
    /// <param name="eventTypeId">The event type ID.</param>
    /// <param name="subscriberId">The subscriber ID sending the webhook.</param>
    /// <param name="payload">The JSON payload.</param>
    /// <param name="scheduledFor">Optional scheduled delivery time.</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created webhook event.</returns>
    Task<WebhookEvent> SendWebhookAsync(
        Guid eventTypeId, 
        Guid subscriberId, 
        string payload, 
        DateTime? scheduledFor = null, 
        string? metadata = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of a webhook event.
    /// </summary>
    /// <param name="webhookEventId">The webhook event ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The webhook event with delivery status information.</returns>
    Task<WebhookEvent?> GetWebhookStatusAsync(Guid webhookEventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates payload against event type schema.
    /// </summary>
    /// <param name="eventTypeId">The event type ID.</param>
    /// <param name="payload">The JSON payload to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if valid, otherwise false.</returns>
    Task<bool> ValidatePayloadAsync(Guid eventTypeId, string payload, CancellationToken cancellationToken = default);
}
