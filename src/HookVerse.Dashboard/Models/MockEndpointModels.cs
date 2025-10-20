namespace HookVerse.Dashboard.Models;

/// <summary>
/// Response for mock endpoint details
/// </summary>
public class MockEndpointResponse
{
    public Guid Id { get; set; }
    public Guid SubscriberId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Url { get; set; } = string.Empty;
    public string UrlPath { get; set; } = string.Empty;
    public int ResponseStatus { get; set; }
    public string? ResponseBody { get; set; }
    public string ResponseContentType { get; set; } = "application/json";
    public int ResponseDelayMs { get; set; }
    public Dictionary<string, string>? ResponseHeaders { get; set; }
    public bool IsActive { get; set; }
    public int RequestCount { get; set; }
    public DateTime? LastRequestAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Request to create a mock endpoint
/// </summary>
public class CreateMockEndpointRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ResponseStatus { get; set; } = 200;
    public string? ResponseBody { get; set; }
    public string ResponseContentType { get; set; } = "application/json";
    public int ResponseDelayMs { get; set; } = 0;
    public Dictionary<string, string>? ResponseHeaders { get; set; }
}

/// <summary>
/// Request to update a mock endpoint
/// </summary>
public class UpdateMockEndpointRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int? ResponseStatus { get; set; }
    public string? ResponseBody { get; set; }
    public string? ResponseContentType { get; set; }
    public int? ResponseDelayMs { get; set; }
    public Dictionary<string, string>? ResponseHeaders { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>
/// Request to trigger a test webhook
/// </summary>
public class TriggerMockWebhookRequest
{
    public string EventType { get; set; } = "test.event";
    public object? Payload { get; set; }
}

/// <summary>
/// Response from triggering a test webhook
/// </summary>
public class TriggerMockWebhookResponse
{
    public Guid MockEndpointId { get; set; }
    public string MockEndpointUrl { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public object? Payload { get; set; }
    public string Message { get; set; } = string.Empty;
}
