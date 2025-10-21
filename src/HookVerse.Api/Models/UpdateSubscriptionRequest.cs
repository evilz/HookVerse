using HookVerse.Core.Enums;

namespace HookVerse.Api.Models;

/// <summary>
/// Request model for updating an existing webhook subscription
/// </summary>
public class UpdateSubscriptionRequest
{
    /// <summary>
    /// The HTTPS endpoint URL where webhooks will be delivered
    /// </summary>
    public string? EndpointUrl { get; set; }

    /// <summary>
    /// Secret key used for HMAC signature generation (min 32 characters)
    /// </summary>
    public string? Secret { get; set; }

    /// <summary>
    /// Authentication type for the webhook endpoint
    /// </summary>
    public AuthType? AuthType { get; set; }

    /// <summary>
    /// Authentication configuration (headers, credentials, etc.)
    /// </summary>
    public string? AuthConfig { get; set; }

    /// <summary>
    /// Optional description for the subscription
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Timeout in seconds for webhook delivery
    /// </summary>
    public int? TimeoutSeconds { get; set; }

    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    public int? MaxRetries { get; set; }
}
