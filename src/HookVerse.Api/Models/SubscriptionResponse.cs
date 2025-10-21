using HookVerse.Core.Enums;

namespace HookVerse.Api.Models;

/// <summary>
/// Response model for webhook subscription details
/// </summary>
public class SubscriptionResponse
{
    /// <summary>
    /// Unique identifier for the subscription
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The subscriber ID that owns this subscription
    /// </summary>
    public Guid SubscriberId { get; set; }

    /// <summary>
    /// The event type ID this subscription listens to
    /// </summary>
    public Guid EventTypeId { get; set; }

    /// <summary>
    /// Name of the event type
    /// </summary>
    public string EventTypeName { get; set; } = string.Empty;

    /// <summary>
    /// The HTTPS endpoint URL where webhooks are delivered
    /// </summary>
    public string EndpointUrl { get; set; } = string.Empty;

    /// <summary>
    /// Authentication type for the webhook endpoint
    /// </summary>
    public AuthType AuthType { get; set; }

    /// <summary>
    /// Optional description for the subscription
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Timeout in seconds for webhook delivery
    /// </summary>
    public int TimeoutSeconds { get; set; }

    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    public int MaxRetries { get; set; }

    /// <summary>
    /// Whether the subscription is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// When the subscription was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the subscription was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// When the subscription was last paused (if applicable)
    /// </summary>
    public DateTime? PausedAt { get; set; }
}
