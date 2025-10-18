using HookVerse.Core.Enums;

namespace HookVerse.Core.Entities;

/// <summary>
/// Represents a subscription to receive webhooks for a specific event type.
/// </summary>
public class Subscription : Entity
{
    /// <summary>
    /// Gets or sets the webhook recipient (subscriber) ID.
    /// </summary>
    public Guid SubscriberId { get; set; }

    /// <summary>
    /// Gets or sets the event type ID to subscribe to.
    /// </summary>
    public Guid EventTypeId { get; set; }

    /// <summary>
    /// Gets or sets the delivery endpoint URL (HTTPS required in production).
    /// </summary>
    public string EndpointUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the HMAC signature secret (encrypted, min 32 chars).
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the subscription is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the JSONPath filter expression.
    /// </summary>
    public string? FilterExpression { get; set; }

    /// <summary>
    /// Gets or sets custom headers (encrypted JSON map).
    /// </summary>
    public string? CustomHeaders { get; set; }

    /// <summary>
    /// Gets or sets the authentication type.
    /// </summary>
    public AuthType AuthType { get; set; } = AuthType.None;

    /// <summary>
    /// Gets or sets the authentication configuration (encrypted JSON).
    /// </summary>
    public string? AuthConfig { get; set; }

    /// <summary>
    /// Gets or sets the last successful delivery timestamp.
    /// </summary>
    public DateTime? LastDeliveryAt { get; set; }

    /// <summary>
    /// Navigation property for the subscriber.
    /// </summary>
    public virtual Subscriber Subscriber { get; set; } = null!;

    /// <summary>
    /// Navigation property for the event type.
    /// </summary>
    public virtual EventType EventType { get; set; } = null!;

    /// <summary>
    /// Navigation property for delivery attempts.
    /// </summary>
    public virtual ICollection<DeliveryAttempt> DeliveryAttempts { get; set; } = new List<DeliveryAttempt>();
}
