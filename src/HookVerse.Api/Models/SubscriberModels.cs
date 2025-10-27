using System.ComponentModel.DataAnnotations;

namespace HookVerse.Api.Models;

/// <summary>
/// Request model for creating a new subscriber
/// </summary>
public class CreateSubscriberRequest
{
    /// <summary>
    /// Name of the subscriber (organization or application name)
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Contact email address (unique)
    /// </summary>
    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Number of days to retain webhook data (default: 90)
    /// </summary>
    [Range(1, 365)]
    public int RetentionDays { get; set; } = 90;
}

/// <summary>
/// Request model for updating an existing subscriber
/// </summary>
public class UpdateSubscriberRequest
{
    /// <summary>
    /// Name of the subscriber
    /// </summary>
    [StringLength(200, MinimumLength = 1)]
    public string? Name { get; set; }

    /// <summary>
    /// Contact email address
    /// </summary>
    [EmailAddress]
    [StringLength(255)]
    public string? Email { get; set; }

    /// <summary>
    /// Number of days to retain webhook data
    /// </summary>
    [Range(1, 365)]
    public int? RetentionDays { get; set; }

    /// <summary>
    /// Whether the subscriber is active
    /// </summary>
    public bool? IsActive { get; set; }
}

/// <summary>
/// Response model for subscriber information
/// </summary>
public class SubscriberResponse
{
    /// <summary>
    /// Unique identifier for the subscriber
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Name of the subscriber
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Contact email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Whether the subscriber is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Number of days to retain webhook data
    /// </summary>
    public int RetentionDays { get; set; }

    /// <summary>
    /// When the subscriber was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the subscriber was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Response model for subscriber creation (includes API key)
/// </summary>
public class SubscriberCreatedResponse : SubscriberResponse
{
    /// <summary>
    /// The generated API key (only visible once at creation)
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Warning message about API key security
    /// </summary>
    public string Warning { get; set; } = "This API key will only be shown once. Please save it securely.";
}

/// <summary>
/// Request model for creating a new API key for a subscriber
/// </summary>
public class CreateApiKeyRequest
{
    /// <summary>
    /// Name/description for this API key
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Response model for API key creation
/// </summary>
public class ApiKeyCreatedResponse
{
    /// <summary>
    /// The unique identifier of the API key
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Name/description of the API key
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The generated API key (only visible once at creation)
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Prefix of the key (for identification)
    /// </summary>
    public string KeyPrefix { get; set; } = string.Empty;

    /// <summary>
    /// When the API key was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Warning message about API key security
    /// </summary>
    public string Warning { get; set; } = "This API key will only be shown once. Please save it securely.";
}

/// <summary>
/// Response model for listing API keys (without the actual key)
/// </summary>
public class ApiKeyListItem
{
    /// <summary>
    /// The unique identifier of the API key
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Name/description of the API key
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Prefix of the key (for identification)
    /// </summary>
    public string KeyPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Whether the API key is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// When the API key was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the API key was last used
    /// </summary>
    public DateTime? LastUsedAt { get; set; }
}
