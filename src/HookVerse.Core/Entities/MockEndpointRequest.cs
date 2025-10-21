namespace HookVerse.Core.Entities;

/// <summary>
/// Represents a request received by a mock endpoint.
/// Stores full request details for inspection and debugging.
/// </summary>
public class MockEndpointRequest : Entity
{
    /// <summary>
    /// Gets or sets the mock endpoint that received this request.
    /// </summary>
    public Guid MockEndpointId { get; set; }

    /// <summary>
    /// Gets or sets the HTTP method used (GET, POST, etc.).
    /// </summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the request path.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the query string.
    /// </summary>
    public string? QueryString { get; set; }

    /// <summary>
    /// Gets or sets the request headers as JSON.
    /// </summary>
    public string? Headers { get; set; }

    /// <summary>
    /// Gets or sets the request body.
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Gets or sets the content type of the request.
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Gets or sets the client IP address.
    /// </summary>
    public string? ClientIp { get; set; }

    /// <summary>
    /// Gets or sets the user agent string.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Gets or sets the HTTP status code returned in the response.
    /// </summary>
    public int ResponseStatus { get; set; }

    /// <summary>
    /// Gets or sets the response body that was returned.
    /// </summary>
    public string? ResponseBody { get; set; }

    /// <summary>
    /// Gets or sets when the request was received.
    /// </summary>
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to MockEndpoint.
    /// </summary>
    public MockEndpoint? MockEndpoint { get; set; }
}
