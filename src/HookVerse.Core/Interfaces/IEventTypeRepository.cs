using HookVerse.Core.Entities;

namespace HookVerse.Core.Interfaces;

/// <summary>
/// Repository interface for EventType entity operations.
/// </summary>
public interface IEventTypeRepository : IRepository<EventType>
{
    /// <summary>
    /// Gets an event type by name and subscriber ID (latest version).
    /// </summary>
    /// <param name="name">The event type name.</param>
    /// <param name="subscriberId">The subscriber ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The event type if found, otherwise null.</returns>
    Task<EventType?> GetByNameAndSubscriberAsync(string name, Guid subscriberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an event type by name, version, and subscriber ID.
    /// </summary>
    /// <param name="name">The event type name.</param>
    /// <param name="version">The event type version.</param>
    /// <param name="subscriberId">The subscriber ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The event type if found, otherwise null.</returns>
    Task<EventType?> GetByNameVersionSubscriberAsync(string name, string version, Guid subscriberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all event types for a subscriber.
    /// </summary>
    /// <param name="subscriberId">The subscriber ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of event types.</returns>
    Task<IEnumerable<EventType>> GetBySubscriberIdAsync(Guid subscriberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active event types.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of active event types.</returns>
    Task<IEnumerable<EventType>> GetActiveEventTypesAsync(CancellationToken cancellationToken = default);
}
