namespace HookVerse.Shared.Contracts;

/// <summary>
/// Message sent when a webhook needs to be delivered
/// </summary>
public record WebhookDeliveryRequested
{
    public Guid WebhookId { get; init; }
    public Guid SubscriptionId { get; init; }
    public Guid TenantId { get; init; }
    public string Url { get; init; } = string.Empty;
    public string HttpMethod { get; init; } = "POST";
    public Dictionary<string, string> Headers { get; init; } = new();
    public string Payload { get; init; } = string.Empty;
    public int RetryCount { get; init; }
    public DateTime RequestedAt { get; init; }
}

/// <summary>
/// Message sent when webhook delivery succeeds
/// </summary>
public record WebhookDeliverySucceeded
{
    public Guid WebhookId { get; init; }
    public Guid DeliveryId { get; init; }
    public int StatusCode { get; init; }
    public string ResponseBody { get; init; } = string.Empty;
    public TimeSpan Duration { get; init; }
    public DateTime CompletedAt { get; init; }
}

/// <summary>
/// Message sent when webhook delivery fails
/// </summary>
public record WebhookDeliveryFailed
{
    public Guid WebhookId { get; init; }
    public Guid DeliveryId { get; init; }
    public string ErrorMessage { get; init; } = string.Empty;
    public int? StatusCode { get; init; }
    public int RetryCount { get; init; }
    public bool WillRetry { get; init; }
    public DateTime? NextRetryAt { get; init; }
    public DateTime FailedAt { get; init; }
}

/// <summary>
/// Message sent to schedule a retry for failed delivery
/// </summary>
public record WebhookRetryScheduled
{
    public Guid WebhookId { get; init; }
    public Guid DeliveryId { get; init; }
    public int RetryAttempt { get; init; }
    public DateTime ScheduledFor { get; init; }
    public TimeSpan RetryDelay { get; init; }
}
