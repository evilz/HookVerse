using HookVerse.Core.Enums;

namespace HookVerse.Api.Models;

/// <summary>
/// Response model for delivery status information.
/// </summary>
public class DeliveryStatusResponse
{
    /// <summary>
    /// Gets or sets the webhook event ID.
    /// </summary>
    public Guid WebhookEventId { get; set; }

    /// <summary>
    /// Gets or sets the event type name.
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the trace ID.
    /// </summary>
    public string TraceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the number of delivery attempts.
    /// </summary>
    public int TotalAttempts { get; set; }

    /// <summary>
    /// Gets or sets the number of successful deliveries.
    /// </summary>
    public int SuccessfulDeliveries { get; set; }

    /// <summary>
    /// Gets or sets the number of failed deliveries.
    /// </summary>
    public int FailedDeliveries { get; set; }

    /// <summary>
    /// Gets or sets the delivery attempts.
    /// </summary>
    public List<DeliveryAttemptDto> Attempts { get; set; } = new();
}

/// <summary>
/// DTO for delivery attempt details.
/// </summary>
public class DeliveryAttemptDto
{
    /// <summary>
    /// Gets or sets the attempt ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the subscription ID.
    /// </summary>
    public Guid SubscriptionId { get; set; }

    /// <summary>
    /// Gets or sets the endpoint URL.
    /// </summary>
    public string EndpointUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the attempt number.
    /// </summary>
    public int AttemptNumber { get; set; }

    /// <summary>
    /// Gets or sets the delivery status.
    /// </summary>
    public DeliveryStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the HTTP response status code.
    /// </summary>
    public int? ResponseStatus { get; set; }

    /// <summary>
    /// Gets or sets the duration in milliseconds.
    /// </summary>
    public int? DurationMs { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the start timestamp.
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the completion timestamp.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the next retry time.
    /// </summary>
    public DateTime? NextRetryAt { get; set; }
}
