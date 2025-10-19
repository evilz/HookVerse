using HookVerse.Core.Entities;

namespace HookVerse.Core.Interfaces;

/// <summary>
/// Repository interface for WebhookEvent entity operations.
/// </summary>
public interface IWebhookEventRepository : IRepository<WebhookEvent>
{
    /// <summary>
    /// Gets all webhook events for a subscriber.
    /// </summary>
    /// <param name="subscriberId">The subscriber ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of webhook events.</returns>
    Task<IEnumerable<WebhookEvent>> GetBySubscriberIdAsync(Guid subscriberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets webhook events scheduled for delivery before a specific time.
    /// </summary>
    /// <param name="beforeTime">The cutoff time.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of webhook events ready for delivery.</returns>
    Task<IEnumerable<WebhookEvent>> GetScheduledForDeliveryAsync(DateTime beforeTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets expired webhook events that should be purged.
    /// </summary>
    /// <param name="beforeTime">The expiry cutoff time.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of expired webhook events.</returns>
    Task<IEnumerable<WebhookEvent>> GetExpiredEventsAsync(DateTime beforeTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a webhook event by ID with its delivery attempts.
    /// </summary>
    /// <param name="id">The webhook event ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The webhook event with delivery attempts if found, otherwise null.</returns>
    Task<WebhookEvent?> GetWithDeliveryAttemptsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches webhook events with filtering and pagination.
    /// </summary>
    /// <param name="subscriberId">The subscriber ID.</param>
    /// <param name="startDate">Optional start date filter.</param>
    /// <param name="endDate">Optional end date filter.</param>
    /// <param name="eventTypeId">Optional event type filter.</param>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tuple of webhook events and total count.</returns>
    Task<(IEnumerable<WebhookEvent> Events, int TotalCount)> SearchAsync(
        Guid subscriberId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        Guid? eventTypeId = null,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default);
}
