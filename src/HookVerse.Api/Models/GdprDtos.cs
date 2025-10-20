using HookVerse.Core.ValueObjects;

namespace HookVerse.Api.Models;

/// <summary>
/// Request to export subscriber data.
/// </summary>
public class CreateGdprExportRequest
{
    /// <summary>
    /// Optional metadata about the request (e.g., requester email, reason)
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Request to delete subscriber data.
/// </summary>
public class CreateGdprDeleteRequest
{
    /// <summary>
    /// Confirmation that the user wants to delete all data
    /// </summary>
    public bool Confirmed { get; set; }

    /// <summary>
    /// Optional metadata about the request (e.g., requester email, reason)
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Response for GDPR request details.
/// </summary>
public class GdprRequestResponse
{
    public Guid Id { get; set; }
    public Guid SubscriberId { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ExportFilePath { get; set; }
    public long? ExportFileSizeBytes { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}
