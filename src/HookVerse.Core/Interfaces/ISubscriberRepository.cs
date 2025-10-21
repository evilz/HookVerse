using HookVerse.Core.Entities;

namespace HookVerse.Core.Interfaces;

/// <summary>
/// Repository interface for Subscriber entity operations.
/// </summary>
public interface ISubscriberRepository : IRepository<Subscriber>
{
    /// <summary>
    /// Gets a subscriber by email address.
    /// </summary>
    /// <param name="email">The email address to search for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The subscriber if found, otherwise null.</returns>
    Task<Subscriber?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a subscriber by API key hash.
    /// </summary>
    /// <param name="apiKeyHash">The hashed API key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The subscriber if found, otherwise null.</returns>
    Task<Subscriber?> GetByApiKeyHashAsync(string apiKeyHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active subscribers.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of active subscribers.</returns>
    Task<IEnumerable<Subscriber>> GetActiveSubscribersAsync(CancellationToken cancellationToken = default);
}
