using HookVerse.Core.Entities;

namespace HookVerse.Core.Interfaces;

/// <summary>
/// Service interface for mock endpoint operations.
/// </summary>
public interface IMockEndpointService
{
    /// <summary>
    /// Creates a new mock endpoint for a subscriber.
    /// </summary>
    /// <param name="subscriberId">The subscriber ID.</param>
    /// <param name="name">Friendly name.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="responseStatus">HTTP status code to return.</param>
    /// <param name="responseBody">Response body content.</param>
    /// <param name="responseContentType">Response content type.</param>
    /// <param name="responseDelayMs">Simulated delay in milliseconds.</param>
    /// <param name="responseHeaders">Custom response headers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created mock endpoint.</returns>
    Task<MockEndpoint> CreateMockEndpointAsync(
        Guid subscriberId,
        string name,
        string? description,
        int responseStatus,
        string? responseBody,
        string responseContentType,
        int responseDelayMs,
        Dictionary<string, string>? responseHeaders,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all mock endpoints for a subscriber.
    /// </summary>
    /// <param name="subscriberId">The subscriber ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of mock endpoints.</returns>
    Task<IEnumerable<MockEndpoint>> GetMockEndpointsAsync(Guid subscriberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific mock endpoint by ID.
    /// </summary>
    /// <param name="id">Mock endpoint ID.</param>
    /// <param name="subscriberId">The subscriber ID (for authorization).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Mock endpoint or null.</returns>
    Task<MockEndpoint?> GetMockEndpointAsync(Guid id, Guid subscriberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a mock endpoint.
    /// </summary>
    /// <param name="id">Mock endpoint ID.</param>
    /// <param name="subscriberId">The subscriber ID (for authorization).</param>
    /// <param name="name">New name (optional).</param>
    /// <param name="description">New description (optional).</param>
    /// <param name="responseStatus">New response status (optional).</param>
    /// <param name="responseBody">New response body (optional).</param>
    /// <param name="responseContentType">New content type (optional).</param>
    /// <param name="responseDelayMs">New delay (optional).</param>
    /// <param name="responseHeaders">New headers (optional).</param>
    /// <param name="isActive">New active status (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated mock endpoint or null if not found.</returns>
    Task<MockEndpoint?> UpdateMockEndpointAsync(
        Guid id,
        Guid subscriberId,
        string? name,
        string? description,
        int? responseStatus,
        string? responseBody,
        string? responseContentType,
        int? responseDelayMs,
        Dictionary<string, string>? responseHeaders,
        bool? isActive,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a mock endpoint.
    /// </summary>
    /// <param name="id">Mock endpoint ID.</param>
    /// <param name="subscriberId">The subscriber ID (for authorization).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteMockEndpointAsync(Guid id, Guid subscriberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles a request to a mock endpoint.
    /// </summary>
    /// <param name="urlPath">The URL path.</param>
    /// <param name="method">HTTP method.</param>
    /// <param name="queryString">Query string.</param>
    /// <param name="headers">Request headers.</param>
    /// <param name="body">Request body.</param>
    /// <param name="contentType">Content type.</param>
    /// <param name="clientIp">Client IP address.</param>
    /// <param name="userAgent">User agent string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Response details (status, body, headers, delay).</returns>
    Task<(int Status, string? Body, string ContentType, Dictionary<string, string>? Headers, int DelayMs)> HandleMockRequestAsync(
        string urlPath,
        string method,
        string? queryString,
        Dictionary<string, string> headers,
        string? body,
        string? contentType,
        string? clientIp,
        string? userAgent,
        CancellationToken cancellationToken = default);
}
