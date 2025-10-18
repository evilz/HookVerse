namespace HookVerse.Core.Entities;

/// <summary>
/// Represents a business event to be delivered as a webhook.
/// </summary>
public class WebhookEvent : Entity
{
    /// <summary>
    /// Gets or sets the event type ID.
    /// </summary>
    public Guid EventTypeId { get; set; }

    /// <summary>
    /// Gets or sets the sender subscriber ID.
    /// </summary>
    public Guid SubscriberId { get; set; }

    /// <summary>
    /// Gets or sets the JSON payload (encrypted at rest).
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the SHA256 hash of the payload.
    /// </summary>
    public string PayloadHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the payload size in bytes (max 1MB).
    /// </summary>
    public int PayloadSizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the W3C distributed trace ID.
    /// </summary>
    public string TraceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the scheduled delivery time (null = immediate).
    /// </summary>
    public DateTime? ScheduledFor { get; set; }

    /// <summary>
    /// Gets or sets the retention expiry date (CreatedAt + RetentionDays).
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets additional metadata (JSON key-value pairs).
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Navigation property for the event type.
    /// </summary>
    public virtual EventType EventType { get; set; } = null!;

    /// <summary>
    /// Navigation property for the sender subscriber.
    /// </summary>
    public virtual Subscriber Subscriber { get; set; } = null!;

    /// <summary>
    /// Navigation property for delivery attempts.
    /// </summary>
    public virtual ICollection<DeliveryAttempt> DeliveryAttempts { get; set; } = new List<DeliveryAttempt>();
}
