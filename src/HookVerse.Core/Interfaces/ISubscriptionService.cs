using HookVerse.Core.Entities;
using HookVerse.Core.Enums;

namespace HookVerse.Core.Interfaces;

/// <summary>
/// Service for managing webhook subscriptions with business rule validation
/// </summary>
public interface ISubscriptionService
{
    /// <summary>
    /// Create a new webhook subscription with validation
    /// </summary>
    /// <param name="subscriberId">The subscriber creating the subscription</param>
    /// <param name="eventTypeId">The event type to subscribe to</param>
    /// <param name="endpointUrl">The webhook endpoint URL (must be HTTPS and not a private IP)</param>
    /// <param name="secret">The secret for HMAC signature (min 32 characters)</param>
    /// <param name="authType">Authentication type for the endpoint</param>
    /// <param name="authConfig">Optional authentication configuration</param>
    /// <param name="description">Optional description</param>
    /// <param name="timeoutSeconds">Timeout for delivery in seconds</param>
    /// <param name="maxRetries">Maximum number of retry attempts</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created subscription</returns>
    /// <exception cref="ArgumentException">Thrown when validation fails</exception>
    Task<Subscription> CreateSubscriptionAsync(
        Guid subscriberId,
        Guid eventTypeId,
        string endpointUrl,
        string secret,
        AuthType authType,
        string? authConfig,
        string? description,
        int timeoutSeconds,
        int maxRetries,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing subscription with validation
    /// </summary>
    /// <param name="subscription">The subscription to update</param>
    /// <param name="endpointUrl">Optional new endpoint URL</param>
    /// <param name="secret">Optional new secret</param>
    /// <param name="authType">Optional new auth type</param>
    /// <param name="authConfig">Optional new auth config</param>
    /// <param name="description">Optional new description</param>
    /// <param name="timeoutSeconds">Optional new timeout</param>
    /// <param name="maxRetries">Optional new max retries</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated subscription</returns>
    /// <exception cref="ArgumentException">Thrown when validation fails</exception>
    Task<Subscription> UpdateSubscriptionAsync(
        Subscription subscription,
        string? endpointUrl,
        string? secret,
        AuthType? authType,
        string? authConfig,
        string? description,
        int? timeoutSeconds,
        int? maxRetries,
        CancellationToken cancellationToken = default);
}
