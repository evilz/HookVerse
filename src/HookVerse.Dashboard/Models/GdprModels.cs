namespace HookVerse.Dashboard.Models;

/// <summary>
/// Response model for GDPR requests
/// </summary>
public class GdprRequestResponse
{
    public Guid Id { get; set; }
    public Guid SubscriberId { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ExportFilePath { get; set; }
    public long? ExportFileSizeBytes { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}
