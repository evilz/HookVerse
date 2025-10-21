namespace HookVerse.Api.Models;

/// <summary>
/// Response model for event type details
/// </summary>
public class EventTypeResponse
{
    /// <summary>
    /// Unique identifier of the event type
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Unique name identifying the event type
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the event type
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// ID of the subscriber who owns this event type
    /// </summary>
    public Guid SubscriberId { get; set; }

    /// <summary>
    /// Indicates whether a schema is defined for this event type
    /// </summary>
    public bool HasSchema { get; set; }

    /// <summary>
    /// When the event type was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the event type was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
