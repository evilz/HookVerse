using HookVerse.Core.Entities;
using HookVerse.Core.Enums;

namespace HookVerse.Core.Interfaces;

/// <summary>
/// Repository interface for DeliveryAttempt entity operations.
/// </summary>
public interface IDeliveryAttemptRepository : IRepository<DeliveryAttempt>
{
    /// <summary>
    /// Gets all delivery attempts for a webhook event.
    /// </summary>
    /// <param name="webhookEventId">The webhook event ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of delivery attempts ordered by attempt number.</returns>
    Task<IEnumerable<DeliveryAttempt>> GetByWebhookEventIdAsync(Guid webhookEventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all delivery attempts for a subscription.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of delivery attempts.</returns>
    Task<IEnumerable<DeliveryAttempt>> GetBySubscriptionIdAsync(Guid subscriptionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets delivery attempts with a specific status that are scheduled for retry.
    /// </summary>
    /// <param name="status">The delivery status to filter by.</param>
    /// <param name="beforeTime">The retry time cutoff.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of delivery attempts ready for retry.</returns>
    Task<IEnumerable<DeliveryAttempt>> GetForRetryAsync(DeliveryStatus status, DateTime beforeTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest delivery attempt for a webhook event.
    /// </summary>
    /// <param name="webhookEventId">The webhook event ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The latest delivery attempt if found, otherwise null.</returns>
    Task<DeliveryAttempt?> GetLatestAttemptAsync(Guid webhookEventId, CancellationToken cancellationToken = default);
}
