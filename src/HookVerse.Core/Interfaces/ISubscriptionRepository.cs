using HookVerse.Core.Entities;

namespace HookVerse.Core.Interfaces;

/// <summary>
/// Repository interface for Subscription entity operations.
/// </summary>
public interface ISubscriptionRepository : IRepository<Subscription>
{
    /// <summary>
    /// Gets all subscriptions for an event type.
    /// </summary>
    /// <param name="eventTypeId">The event type ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of subscriptions.</returns>
    Task<IEnumerable<Subscription>> GetByEventTypeIdAsync(Guid eventTypeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active subscriptions for an event type.
    /// </summary>
    /// <param name="eventTypeId">The event type ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of active subscriptions.</returns>
    Task<IEnumerable<Subscription>> GetActiveByEventTypeIdAsync(Guid eventTypeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all subscriptions for a subscriber.
    /// </summary>
    /// <param name="subscriberId">The subscriber ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of subscriptions.</returns>
    Task<IEnumerable<Subscription>> GetBySubscriberIdAsync(Guid subscriberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the last delivery timestamp for a subscription.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <param name="timestamp">The delivery timestamp.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateLastDeliveryAsync(Guid subscriptionId, DateTime timestamp, CancellationToken cancellationToken = default);
}
