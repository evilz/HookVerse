namespace HookVerse.Core.Entities;

/// <summary>
/// Represents a tenant/customer who sends webhooks.
/// </summary>
public class Subscriber : Entity
{
    /// <summary>
    /// Gets or sets the subscriber name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the contact email address (unique).
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the hashed API key (SHA256).
    /// </summary>
    public string ApiKeyHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the subscriber is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the event retention period in days (1-3650, default 90).
    /// </summary>
    public int RetentionDays { get; set; } = 90;

    /// <summary>
    /// Navigation property for event types owned by this subscriber.
    /// </summary>
    public virtual ICollection<EventType> EventTypes { get; set; } = new List<EventType>();

    /// <summary>
    /// Navigation property for webhook events sent by this subscriber.
    /// </summary>
    public virtual ICollection<WebhookEvent> WebhookEvents { get; set; } = new List<WebhookEvent>();

    /// <summary>
    /// Navigation property for subscriptions owned by this subscriber.
    /// </summary>
    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
