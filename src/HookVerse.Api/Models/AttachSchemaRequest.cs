using HookVerse.Core.Enums;

namespace HookVerse.Api.Models;

/// <summary>
/// Request model for attaching or updating a schema for an event type
/// </summary>
public class AttachSchemaRequest
{
    /// <summary>
    /// The schema format (JsonSchema, Avro, Protobuf, DotNetAssembly)
    /// </summary>
    public SchemaFormat Format { get; set; }

    /// <summary>
    /// The schema content as a string
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of this schema version
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Version number for this schema (defaults to 1)
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Whether this schema should be marked as active (defaults to true)
    /// </summary>
    public bool IsActive { get; set; } = true;
}
