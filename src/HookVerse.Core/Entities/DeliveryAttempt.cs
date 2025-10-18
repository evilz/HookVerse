using HookVerse.Core.Enums;

namespace HookVerse.Core.Entities;

/// <summary>
/// Records individual webhook delivery attempts with full request/response details.
/// </summary>
public class DeliveryAttempt : Entity
{
    /// <summary>
    /// Gets or sets the associated webhook event ID.
    /// </summary>
    public Guid WebhookEventId { get; set; }

    /// <summary>
    /// Gets or sets the target subscription ID.
    /// </summary>
    public Guid SubscriptionId { get; set; }

    /// <summary>
    /// Gets or sets the attempt sequence number (1-10).
    /// </summary>
    public int AttemptNumber { get; set; }

    /// <summary>
    /// Gets or sets the delivery outcome status.
    /// </summary>
    public DeliveryStatus Status { get; set; } = DeliveryStatus.Pending;

    /// <summary>
    /// Gets or sets the attempt start timestamp.
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the attempt completion timestamp.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the duration in milliseconds.
    /// </summary>
    public int? DurationMs { get; set; }

    /// <summary>
    /// Gets or sets the HTTP request headers (JSON).
    /// </summary>
    public string? RequestHeaders { get; set; }

    /// <summary>
    /// Gets or sets the HTTP request body.
    /// </summary>
    public string? RequestBody { get; set; }

    /// <summary>
    /// Gets or sets the HTTP response status code.
    /// </summary>
    public int? ResponseStatus { get; set; }

    /// <summary>
    /// Gets or sets the HTTP response headers (JSON).
    /// </summary>
    public string? ResponseHeaders { get; set; }

    /// <summary>
    /// Gets or sets the HTTP response body (truncated to 10KB).
    /// </summary>
    public string? ResponseBody { get; set; }

    /// <summary>
    /// Gets or sets the error description (max 1000 chars).
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the HMAC signature sent.
    /// </summary>
    public string Signature { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the distributed trace ID.
    /// </summary>
    public string TraceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the scheduled retry time.
    /// </summary>
    public DateTime? NextRetryAt { get; set; }

    /// <summary>
    /// Navigation property for the webhook event.
    /// </summary>
    public virtual WebhookEvent WebhookEvent { get; set; } = null!;

    /// <summary>
    /// Navigation property for the subscription.
    /// </summary>
    public virtual Subscription Subscription { get; set; } = null!;
}
