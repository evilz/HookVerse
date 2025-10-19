using HookVerse.Core.Entities;

namespace HookVerse.Core.Interfaces;

/// <summary>
/// Repository interface for MockEndpoint entity operations.
/// </summary>
public interface IMockEndpointRepository : IRepository<MockEndpoint>
{
    /// <summary>
    /// Gets all mock endpoints for a subscriber.
    /// </summary>
    /// <param name="subscriberId">The subscriber ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of mock endpoints.</returns>
    Task<IEnumerable<MockEndpoint>> GetBySubscriberIdAsync(Guid subscriberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a mock endpoint by its URL path.
    /// </summary>
    /// <param name="urlPath">The URL path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Mock endpoint or null.</returns>
    Task<MockEndpoint?> GetByUrlPathAsync(string urlPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active mock endpoints for a subscriber.
    /// </summary>
    /// <param name="subscriberId">The subscriber ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of active mock endpoints.</returns>
    Task<IEnumerable<MockEndpoint>> GetActiveBySubscriberIdAsync(Guid subscriberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Increments the request count for a mock endpoint.
    /// </summary>
    /// <param name="mockEndpointId">The mock endpoint ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task IncrementRequestCountAsync(Guid mockEndpointId, CancellationToken cancellationToken = default);
}
