using HookVerse.Core.Enums;

namespace HookVerse.Api.Models;

/// <summary>
/// Request model for creating a new webhook subscription
/// </summary>
public class CreateSubscriptionRequest
{
    /// <summary>
    /// The event type ID to subscribe to
    /// </summary>
    public Guid EventTypeId { get; set; }

    /// <summary>
    /// The HTTPS endpoint URL where webhooks will be delivered
    /// </summary>
    public string EndpointUrl { get; set; } = string.Empty;

    /// <summary>
    /// Secret key used for HMAC signature generation (min 32 characters)
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// Authentication type for the webhook endpoint
    /// </summary>
    public AuthType AuthType { get; set; } = AuthType.None;

    /// <summary>
    /// Authentication configuration (headers, credentials, etc.)
    /// </summary>
    public string? AuthConfig { get; set; }

    /// <summary>
    /// Optional description for the subscription
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Timeout in seconds for webhook delivery (default: 30)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum number of retry attempts (default: 5)
    /// </summary>
    public int MaxRetries { get; set; } = 5;
}
