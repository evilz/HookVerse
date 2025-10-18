namespace HookVerse.Core.Entities;

/// <summary>
/// Represents a business event type with versioning support.
/// </summary>
public class EventType : Entity
{
    /// <summary>
    /// Gets or sets the event type name (lowercase dot-notation, e.g., "order.created").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the event type.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the semantic version (e.g., "1.0.0").
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets the owner subscriber ID.
    /// </summary>
    public Guid SubscriberId { get; set; }

    /// <summary>
    /// Gets or sets whether the event type is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Navigation property for the owner subscriber.
    /// </summary>
    public virtual Subscriber Subscriber { get; set; } = null!;

    /// <summary>
    /// Navigation property for the optional schema definition.
    /// </summary>
    public virtual SchemaDefinition? SchemaDefinition { get; set; }

    /// <summary>
    /// Navigation property for webhook events of this type.
    /// </summary>
    public virtual ICollection<WebhookEvent> WebhookEvents { get; set; } = new List<WebhookEvent>();

    /// <summary>
    /// Navigation property for subscriptions to this event type.
    /// </summary>
    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
