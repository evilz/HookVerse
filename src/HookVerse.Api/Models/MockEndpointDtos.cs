namespace HookVerse.Api.Models;

/// <summary>
/// Request to create a new mock endpoint.
/// </summary>
public class CreateMockEndpointRequest
{
    /// <summary>
    /// Friendly name for the mock endpoint.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the mock endpoint's purpose.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// HTTP status code to return (default: 200).
    /// </summary>
    public int ResponseStatus { get; set; } = 200;

    /// <summary>
    /// Response body content.
    /// </summary>
    public string? ResponseBody { get; set; }

    /// <summary>
    /// Response content type (default: application/json).
    /// </summary>
    public string ResponseContentType { get; set; } = "application/json";

    /// <summary>
    /// Simulated response delay in milliseconds (default: 0).
    /// </summary>
    public int ResponseDelayMs { get; set; } = 0;

    /// <summary>
    /// Custom response headers as dictionary.
    /// </summary>
    public Dictionary<string, string>? ResponseHeaders { get; set; }
}

/// <summary>
/// Request to update a mock endpoint.
/// </summary>
public class UpdateMockEndpointRequest
{
    /// <summary>
    /// Friendly name for the mock endpoint.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// HTTP status code to return.
    /// </summary>
    public int? ResponseStatus { get; set; }

    /// <summary>
    /// Response body content.
    /// </summary>
    public string? ResponseBody { get; set; }

    /// <summary>
    /// Response content type.
    /// </summary>
    public string? ResponseContentType { get; set; }

    /// <summary>
    /// Simulated response delay in milliseconds.
    /// </summary>
    public int? ResponseDelayMs { get; set; }

    /// <summary>
    /// Custom response headers.
    /// </summary>
    public Dictionary<string, string>? ResponseHeaders { get; set; }

    /// <summary>
    /// Whether the mock endpoint is active.
    /// </summary>
    public bool? IsActive { get; set; }
}

/// <summary>
/// Response containing mock endpoint details.
/// </summary>
public class MockEndpointResponse
{
    /// <summary>
    /// Mock endpoint ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Subscriber ID who owns this mock endpoint.
    /// </summary>
    public Guid SubscriberId { get; set; }

    /// <summary>
    /// Friendly name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Full URL to the mock endpoint.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// URL path only.
    /// </summary>
    public string UrlPath { get; set; } = string.Empty;

    /// <summary>
    /// HTTP status code to return.
    /// </summary>
    public int ResponseStatus { get; set; }

    /// <summary>
    /// Response body content.
    /// </summary>
    public string? ResponseBody { get; set; }

    /// <summary>
    /// Response content type.
    /// </summary>
    public string ResponseContentType { get; set; } = string.Empty;

    /// <summary>
    /// Simulated response delay in milliseconds.
    /// </summary>
    public int ResponseDelayMs { get; set; }

    /// <summary>
    /// Custom response headers.
    /// </summary>
    public Dictionary<string, string>? ResponseHeaders { get; set; }

    /// <summary>
    /// Whether the mock endpoint is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Total number of requests received.
    /// </summary>
    public int RequestCount { get; set; }

    /// <summary>
    /// When the last request was received.
    /// </summary>
    public DateTime? LastRequestAt { get; set; }

    /// <summary>
    /// When the mock endpoint was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the mock endpoint was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Request to trigger a test webhook to a mock endpoint.
/// </summary>
public class TriggerMockWebhookRequest
{
    /// <summary>
    /// Event type for the test webhook.
    /// </summary>
    public string EventType { get; set; } = "test.event";

    /// <summary>
    /// Custom payload for the test webhook.
    /// </summary>
    public object? Payload { get; set; }
}
