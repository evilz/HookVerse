using HookVerse.Core.Entities;
using HookVerse.Core.Enums;

namespace HookVerse.Core.Interfaces;

/// <summary>
/// Service interface for webhook delivery operations.
/// </summary>
public interface IDeliveryService
{
    /// <summary>
    /// Delivers a webhook to a subscription endpoint.
    /// </summary>
    /// <param name="webhookEvent">The webhook event to deliver.</param>
    /// <param name="subscription">The target subscription.</param>
    /// <param name="attemptNumber">The attempt number (1-based).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The delivery attempt record.</returns>
    Task<DeliveryAttempt> DeliverWebhookAsync(
        WebhookEvent webhookEvent, 
        Subscription subscription, 
        int attemptNumber, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if an endpoint URL is safe to call (SSRF prevention).
    /// </summary>
    /// <param name="url">The URL to validate.</param>
    /// <returns>True if safe, otherwise false.</returns>
    bool IsEndpointSafe(string url);
}
