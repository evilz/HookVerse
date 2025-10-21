using HookVerse.Core.Entities;

namespace HookVerse.Core.Interfaces;

/// <summary>
/// Repository interface for SchemaDefinition entity operations
/// </summary>
public interface ISchemaDefinitionRepository : IRepository<SchemaDefinition>
{
    /// <summary>
    /// Gets the schema definition for a specific event type
    /// </summary>
    /// <param name="eventTypeId">The event type ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The schema definition if found, otherwise null</returns>
    Task<SchemaDefinition?> GetByEventTypeIdAsync(Guid eventTypeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active schema definition for a specific event type
    /// </summary>
    /// <param name="eventTypeId">The event type ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The active schema definition if found, otherwise null</returns>
    Task<SchemaDefinition?> GetActiveByEventTypeIdAsync(Guid eventTypeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a schema definition exists for an event type
    /// </summary>
    /// <param name="eventTypeId">The event type ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if schema exists, otherwise false</returns>
    Task<bool> ExistsForEventTypeAsync(Guid eventTypeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all schema definitions for a subscriber (across all event types)
    /// </summary>
    /// <param name="subscriberId">The subscriber ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of schema definitions</returns>
    Task<IEnumerable<SchemaDefinition>> GetBySubscriberIdAsync(Guid subscriberId, CancellationToken cancellationToken = default);
}
