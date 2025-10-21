using HookVerse.Core.Enums;

namespace HookVerse.Api.Models;

/// <summary>
/// Response model for schema details
/// </summary>
public class SchemaResponse
{
    /// <summary>
    /// Unique identifier of the schema
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The event type ID this schema belongs to
    /// </summary>
    public Guid EventTypeId { get; set; }

    /// <summary>
    /// The schema format
    /// </summary>
    public SchemaFormat Format { get; set; }

    /// <summary>
    /// The schema content as a string
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// SHA-256 hash of the content for versioning
    /// </summary>
    public string ContentHash { get; set; } = string.Empty;

    /// <summary>
    /// Schema version number
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Whether this schema is currently active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Optional description of this schema
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// When the schema was last validated
    /// </summary>
    public DateTime? LastValidatedAt { get; set; }

    /// <summary>
    /// When the schema was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the schema was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
