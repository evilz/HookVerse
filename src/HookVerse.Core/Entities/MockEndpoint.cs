using HookVerse.Core.ValueObjects;

namespace HookVerse.Core.Entities;

/// <summary>
/// Represents a mock webhook endpoint for testing purposes.
/// Allows developers to create test endpoints with configurable responses.
/// </summary>
public class MockEndpoint : Entity
{
    /// <summary>
    /// Gets or sets the subscriber who owns this mock endpoint.
    /// </summary>
    public Guid SubscriberId { get; set; }

    /// <summary>
    /// Gets or sets the friendly name for the mock endpoint.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the mock endpoint's purpose.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the generated URL path for this mock endpoint.
    /// Format: /mock/{unique-id}
    /// </summary>
    public string UrlPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the HTTP status code to return (default: 200).
    /// </summary>
    public int ResponseStatus { get; set; } = 200;

    /// <summary>
    /// Gets or sets the response body content.
    /// </summary>
    public string? ResponseBody { get; set; }

    /// <summary>
    /// Gets or sets the response content type (default: application/json).
    /// </summary>
    public string ResponseContentType { get; set; } = "application/json";

    /// <summary>
    /// Gets or sets the simulated response delay in milliseconds (default: 0).
    /// </summary>
    public int ResponseDelayMs { get; set; } = 0;

    /// <summary>
    /// Gets or sets custom response headers as JSON.
    /// </summary>
    public string? ResponseHeaders { get; set; }

    /// <summary>
    /// Gets or sets whether this mock endpoint is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the total number of requests received by this mock endpoint.
    /// </summary>
    public int RequestCount { get; set; } = 0;

    /// <summary>
    /// Gets or sets when the last request was received.
    /// </summary>
    public DateTime? LastRequestAt { get; set; }

    /// <summary>
    /// Navigation property to Subscriber.
    /// </summary>
    public Subscriber? Subscriber { get; set; }

    /// <summary>
    /// Navigation property to mock endpoint requests.
    /// </summary>
    public ICollection<MockEndpointRequest> Requests { get; set; } = new List<MockEndpointRequest>();
}
