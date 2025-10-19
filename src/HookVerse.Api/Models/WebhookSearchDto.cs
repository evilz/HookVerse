namespace HookVerse.Api.Models;

/// <summary>
/// DTO for webhook search results in the list view.
/// </summary>
public class WebhookSearchItemDto
{
    /// <summary>
    /// Webhook event ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Event type ID.
    /// </summary>
    public Guid EventTypeId { get; set; }

    /// <summary>
    /// Event type name.
    /// </summary>
    public string EventTypeName { get; set; } = string.Empty;

    /// <summary>
    /// Trace ID for correlation.
    /// </summary>
    public string TraceId { get; set; } = string.Empty;

    /// <summary>
    /// When the webhook event was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the webhook is scheduled for delivery.
    /// </summary>
    public DateTime? ScheduledFor { get; set; }

    /// <summary>
    /// Size of the payload in bytes.
    /// </summary>
    public int PayloadSizeBytes { get; set; }

    /// <summary>
    /// Total number of delivery attempts.
    /// </summary>
    public int TotalAttempts { get; set; }

    /// <summary>
    /// Number of successful delivery attempts.
    /// </summary>
    public int SuccessfulAttempts { get; set; }

    /// <summary>
    /// Overall status: Pending, InProgress, Delivered, Failed.
    /// </summary>
    public string Status { get; set; } = string.Empty;
}
