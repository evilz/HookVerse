namespace HookVerse.Core.Interfaces;

/// <summary>
/// Result of API key validation
/// </summary>
public record ApiKeyValidationResult
{
    public bool IsValid { get; init; }
    public Guid? TenantId { get; init; }
    public Guid? ApiKeyId { get; init; }
    public string? ErrorMessage { get; init; }

    public static ApiKeyValidationResult Success(Guid tenantId, Guid apiKeyId) =>
        new() { IsValid = true, TenantId = tenantId, ApiKeyId = apiKeyId };

    public static ApiKeyValidationResult Failure(string errorMessage) =>
        new() { IsValid = false, ErrorMessage = errorMessage };
}

/// <summary>
/// Service for API key management and validation
/// </summary>
public interface IApiKeyService
{
    /// <summary>
    /// Validate an API key
    /// </summary>
    Task<ApiKeyValidationResult> ValidateApiKeyAsync(string apiKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate a new API key for a tenant
    /// </summary>
    Task<string> GenerateApiKeyAsync(Guid tenantId, string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke an API key
    /// </summary>
    Task<bool> RevokeApiKeyAsync(Guid apiKeyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all API keys for a tenant
    /// </summary>
    Task<IEnumerable<ApiKeyInfo>> GetApiKeysAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get API key by hashed key
    /// </summary>
    Task<Core.Entities.ApiKey?> GetApiKeyByHashAsync(string hashedKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all API keys for a tenant (returns full entities)
    /// </summary>
    Task<List<Core.Entities.ApiKey>> GetApiKeysForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Hash an API key for storage
    /// </summary>
    string HashApiKey(string apiKey);
}

/// <summary>
/// Information about an API key (without exposing the actual key)
/// </summary>
public record ApiKeyInfo
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? LastUsedAt { get; init; }
    public bool IsActive { get; init; }
    public string KeyPrefix { get; init; } = string.Empty; // First few characters for identification
}
