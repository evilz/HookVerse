namespace HookVerse.Dashboard.Models;

/// <summary>
/// Webhook search result with pagination
/// </summary>
public class WebhookSearchResult
{
    public List<WebhookListItem> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

/// <summary>
/// Webhook list item for search results
/// </summary>
public class WebhookListItem
{
    public Guid Id { get; set; }
    public Guid EventTypeId { get; set; }
    public string EventTypeName { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ScheduledFor { get; set; }
    public int PayloadSizeBytes { get; set; }
    public int TotalAttempts { get; set; }
    public int SuccessfulAttempts { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Detailed webhook information
/// </summary>
public class WebhookDetailResponse
{
    public Guid Id { get; set; }
    public Guid EventTypeId { get; set; }
    public string EventTypeName { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string PayloadHash { get; set; } = string.Empty;
    public int PayloadSizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ScheduledFor { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? Metadata { get; set; }
    public List<DeliveryAttemptInfo> DeliveryAttempts { get; set; } = new();
}

/// <summary>
/// Delivery attempt information
/// </summary>
public class DeliveryAttemptInfo
{
    public Guid Id { get; set; }
    public Guid SubscriptionId { get; set; }
    public string EndpointUrl { get; set; } = string.Empty;
    public int AttemptNumber { get; set; }
    public int? ResponseStatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public string? ErrorMessage { get; set; }
    public int ResponseTimeMs { get; set; }
    public DateTime AttemptedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Dashboard analytics data
/// </summary>
public class DashboardAnalytics
{
    public int TotalWebhooks { get; set; }
    public int SuccessfulDeliveries { get; set; }
    public int FailedDeliveries { get; set; }
    public int PendingDeliveries { get; set; }
    public double SuccessRate { get; set; }
    public double AverageResponseTimeMs { get; set; }
    public List<EventTypeStats> TopEventTypes { get; set; } = new();
    public List<DailyStats> DailyTrend { get; set; } = new();
}

/// <summary>
/// Event type statistics
/// </summary>
public class EventTypeStats
{
    public Guid EventTypeId { get; set; }
    public string EventTypeName { get; set; } = string.Empty;
    public int Count { get; set; }
    public double SuccessRate { get; set; }
}

/// <summary>
/// Daily statistics
/// </summary>
public class DailyStats
{
    public DateTime Date { get; set; }
    public int TotalWebhooks { get; set; }
    public int SuccessfulDeliveries { get; set; }
    public int FailedDeliveries { get; set; }
}

/// <summary>
/// Event type information
/// </summary>
public class EventTypeInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
