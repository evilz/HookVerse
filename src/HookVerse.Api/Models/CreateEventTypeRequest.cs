namespace HookVerse.Api.Models;

/// <summary>
/// Request model for creating a new event type
/// </summary>
public class CreateEventTypeRequest
{
    /// <summary>
    /// Unique name identifying the event type (e.g., "user.created")
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
}
