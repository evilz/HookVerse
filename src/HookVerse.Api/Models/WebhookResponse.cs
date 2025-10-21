namespace HookVerse.Api.Models;

/// <summary>
/// Response model for webhook operations.
/// </summary>
public class WebhookResponse
{
    /// <summary>
    /// Gets or sets the webhook event ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the event type ID.
    /// </summary>
    public Guid EventTypeId { get; set; }

    /// <summary>
    /// Gets or sets the trace ID for distributed tracing.
    /// </summary>
    public string TraceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the scheduled delivery time.
    /// </summary>
    public DateTime? ScheduledFor { get; set; }

    /// <summary>
    /// Gets or sets the expiry time.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the payload size in bytes.
    /// </summary>
    public int PayloadSizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the status message.
    /// </summary>
    public string Status { get; set; } = "accepted";
}
