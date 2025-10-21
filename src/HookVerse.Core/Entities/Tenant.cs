namespace HookVerse.Core.Entities;

/// <summary>
/// Represents an API key for tenant authentication
/// </summary>
public class ApiKey : Entity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string HashedKey { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    // Navigation property
    public Tenant? Tenant { get; set; }
}

/// <summary>
/// Represents a tenant in the system
/// </summary>
public class Tenant : Entity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    // Navigation properties
    public ICollection<ApiKey> ApiKeys { get; set; } = new List<ApiKey>();
}
