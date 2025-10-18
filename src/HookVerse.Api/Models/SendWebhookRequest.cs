namespace HookVerse.Api.Models;

/// <summary>
/// Request model for sending a webhook.
/// </summary>
public class SendWebhookRequest
{
    /// <summary>
    /// Gets or sets the event type ID.
    /// </summary>
    public Guid EventTypeId { get; set; }

    /// <summary>
    /// Gets or sets the JSON payload.
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional scheduled delivery time.
    /// </summary>
    public DateTime? ScheduledFor { get; set; }

    /// <summary>
    /// Gets or sets optional metadata (JSON).
    /// </summary>
    public string? Metadata { get; set; }
}
