using HookVerse.Core.Enums;

namespace HookVerse.Core.Entities;

/// <summary>
/// Represents a schema definition for event validation.
/// </summary>
public class SchemaDefinition : Entity
{
    /// <summary>
    /// Gets or sets the event type ID (1:1 relationship).
    /// </summary>
    public Guid EventTypeId { get; set; }

    /// <summary>
    /// Gets or sets the schema format type.
    /// </summary>
    public SchemaFormat Format { get; set; }

    /// <summary>
    /// Gets or sets the schema content.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the SHA256 hash of the content.
    /// </summary>
    public string ContentHash { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property for the associated event type.
    /// </summary>
    public virtual EventType EventType { get; set; } = null!;
}
