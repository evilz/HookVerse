using HookVerse.Core.ValueObjects;

namespace HookVerse.Core.Entities;

/// <summary>
/// Represents a GDPR compliance request (export or delete).
/// </summary>
public class GdprRequest : Entity
{
    /// <summary>
    /// The subscriber making the request
    /// </summary>
    public Guid SubscriberId { get; set; }

    /// <summary>
    /// Navigation property to Subscriber
    /// </summary>
    public virtual Subscriber Subscriber { get; set; } = null!;

    /// <summary>
    /// Type of GDPR request
    /// </summary>
    public GdprRequestType RequestType { get; set; }

    /// <summary>
    /// Current status of the request
    /// </summary>
    public GdprRequestStatus Status { get; set; }

    /// <summary>
    /// When the request was completed (if applicable)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Error message if request failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// For export requests: path to the generated export file
    /// </summary>
    public string? ExportFilePath { get; set; }

    /// <summary>
    /// For export requests: size of the export file in bytes
    /// </summary>
    public long? ExportFileSizeBytes { get; set; }

    /// <summary>
    /// Additional metadata about the request (JSON)
    /// </summary>
    public string? Metadata { get; set; }
}
