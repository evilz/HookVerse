using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using HookVerse.Core.Entities;
using HookVerse.Core.Interfaces;

namespace HookVerse.Infrastructure.Services;

/// <summary>
/// Configuration options for API key rotation
/// </summary>
public class ApiKeyRotationOptions
{
    /// <summary>
    /// Maximum lifetime of an API key before it should be rotated (default: 90 days)
    /// </summary>
    public TimeSpan MaxKeyLifetime { get; set; } = TimeSpan.FromDays(90);

    /// <summary>
    /// Grace period where both old and new keys are valid during rotation (default: 7 days)
    /// </summary>
    public TimeSpan RotationGracePeriod { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// How far in advance to warn about upcoming key expiration (default: 14 days)
    /// </summary>
    public TimeSpan ExpirationWarningPeriod { get; set; } = TimeSpan.FromDays(14);

    /// <summary>
    /// Whether to automatically rotate keys when they reach MaxKeyLifetime
    /// </summary>
    public bool EnableAutomaticRotation { get; set; } = true;

    /// <summary>
    /// Whether to automatically revoke old keys after the grace period
    /// </summary>
    public bool AutoRevokeAfterGracePeriod { get; set; } = true;
}

/// <summary>
/// Result of a key rotation operation
/// </summary>
public class ApiKeyRotationResult
{
    public bool Success { get; set; }
    public string? NewApiKey { get; set; }
    public Guid? NewKeyId { get; set; }
    public Guid? OldKeyId { get; set; }
    public DateTime? GracePeriodEndsAt { get; set; }
    public string? Message { get; set; }
    public List<string> Warnings { get; set; } = new();

    public static ApiKeyRotationResult Successful(
        string newApiKey, 
        Guid newKeyId, 
        Guid oldKeyId, 
        DateTime gracePeriodEndsAt)
    {
        return new ApiKeyRotationResult
        {
            Success = true,
            NewApiKey = newApiKey,
            NewKeyId = newKeyId,
            OldKeyId = oldKeyId,
            GracePeriodEndsAt = gracePeriodEndsAt,
            Message = "API key rotated successfully"
        };
    }

    public static ApiKeyRotationResult Failed(string message)
    {
        return new ApiKeyRotationResult
        {
            Success = false,
            Message = message
        };
    }
}

/// <summary>
/// Information about API key rotation status
/// </summary>
public class ApiKeyRotationStatus
{
    public Guid ApiKeyId { get; set; }
    public string KeyName { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public TimeSpan Age { get; set; }
    public TimeSpan TimeUntilExpiration { get; set; }
    public bool RequiresRotation { get; set; }
    public bool InGracePeriod { get; set; }
    public DateTime? GracePeriodEndsAt { get; set; }
    public RotationUrgency Urgency { get; set; }
}

/// <summary>
/// Urgency level for key rotation
/// </summary>
public enum RotationUrgency
{
    None,       // Key is healthy, no rotation needed
    Low,        // Key is aging but not urgent
    Medium,     // Key should be rotated soon
    High,       // Key should be rotated immediately
    Critical    // Key has exceeded maximum lifetime
}

/// <summary>
/// Service for managing API key rotation with graceful transition periods
/// </summary>
public class ApiKeyRotationService
{
    private readonly IRepository<ApiKey> _apiKeyRepository;
    private readonly IApiKeyService _apiKeyService;
    private readonly ILogger<ApiKeyRotationService> _logger;
    private readonly ApiKeyRotationOptions _options;

    public ApiKeyRotationService(
        IRepository<ApiKey> apiKeyRepository,
        IApiKeyService apiKeyService,
        ILogger<ApiKeyRotationService> logger,
        IOptions<ApiKeyRotationOptions> options)
    {
        _apiKeyRepository = apiKeyRepository ?? throw new ArgumentNullException(nameof(apiKeyRepository));
        _apiKeyService = apiKeyService ?? throw new ArgumentNullException(nameof(apiKeyService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Rotates an API key, creating a new key while keeping the old one valid during grace period
    /// </summary>
    public async Task<ApiKeyRotationResult> RotateApiKeyAsync(
        Guid apiKeyId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var oldKey = await _apiKeyRepository.GetByIdAsync(apiKeyId, cancellationToken);
            
            if (oldKey == null)
            {
                return ApiKeyRotationResult.Failed($"API key {apiKeyId} not found");
            }

            if (!oldKey.IsActive)
            {
                return ApiKeyRotationResult.Failed("Cannot rotate an inactive API key");
            }

            _logger.LogInformation(
                "Starting rotation for API key {KeyId} ({KeyName}) for tenant {TenantId}",
                apiKeyId, oldKey.Name, oldKey.TenantId);

            // Generate new API key with rotation suffix
            var newKeyName = $"{oldKey.Name} (Rotated {DateTime.UtcNow:yyyy-MM-dd})";
            var newApiKey = await _apiKeyService.GenerateApiKeyAsync(
                oldKey.TenantId, 
                newKeyName, 
                cancellationToken);

            // Get the newly created key to obtain its ID
            var allKeys = await _apiKeyRepository.FindAsync(
                k => k.TenantId == oldKey.TenantId && k.Name == newKeyName,
                cancellationToken);
            var newKey = allKeys.FirstOrDefault();

            if (newKey == null)
            {
                return ApiKeyRotationResult.Failed("Failed to create new API key");
            }

            // Calculate grace period end date
            var gracePeriodEndsAt = DateTime.UtcNow.Add(_options.RotationGracePeriod);

            // Mark old key with rotation metadata (using a custom approach since we can't add properties)
            // We'll update the name to indicate it's in grace period
            oldKey.Name = $"{oldKey.Name} (Grace period until {gracePeriodEndsAt:yyyy-MM-dd})";
            await _apiKeyRepository.UpdateAsync(oldKey, cancellationToken);
            await _apiKeyRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully rotated API key {OldKeyId} to {NewKeyId}. Grace period ends at {GracePeriodEndsAt}",
                oldKey.Id, newKey.Id, gracePeriodEndsAt);

            var result = ApiKeyRotationResult.Successful(
                newApiKey, 
                newKey.Id, 
                oldKey.Id, 
                gracePeriodEndsAt);

            result.Warnings.Add(
                $"The old API key will remain valid until {gracePeriodEndsAt:yyyy-MM-dd HH:mm:ss} UTC");
            result.Warnings.Add(
                "Update your applications to use the new API key before the grace period ends");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rotating API key {KeyId}", apiKeyId);
            return ApiKeyRotationResult.Failed($"Error rotating API key: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets rotation status for all API keys belonging to a tenant
    /// </summary>
    public async Task<IEnumerable<ApiKeyRotationStatus>> GetRotationStatusAsync(
        Guid tenantId, 
        CancellationToken cancellationToken = default)
    {
        var apiKeys = await _apiKeyRepository.FindAsync(
            k => k.TenantId == tenantId && k.IsActive,
            cancellationToken);

        var now = DateTime.UtcNow;
        var statuses = new List<ApiKeyRotationStatus>();

        foreach (var key in apiKeys)
        {
            var age = now - key.CreatedAt;
            var timeUntilExpiration = _options.MaxKeyLifetime - age;

            var status = new ApiKeyRotationStatus
            {
                ApiKeyId = key.Id,
                KeyName = key.Name,
                KeyPrefix = key.KeyPrefix,
                CreatedAt = key.CreatedAt,
                LastUsedAt = key.LastUsedAt,
                Age = age,
                TimeUntilExpiration = timeUntilExpiration
            };

            // Determine if key is in grace period (check name pattern)
            status.InGracePeriod = key.Name.Contains("Grace period until");
            if (status.InGracePeriod)
            {
                // Try to extract grace period end date from name
                // This is a workaround; ideally we'd have a dedicated field
                var match = System.Text.RegularExpressions.Regex.Match(
                    key.Name, 
                    @"Grace period until (\d{4}-\d{2}-\d{2})");
                if (match.Success && DateTime.TryParse(match.Groups[1].Value, out var endDate))
                {
                    status.GracePeriodEndsAt = endDate;
                }
            }

            // Determine rotation urgency
            if (timeUntilExpiration.TotalDays <= 0)
            {
                status.Urgency = RotationUrgency.Critical;
                status.RequiresRotation = true;
            }
            else if (timeUntilExpiration <= _options.ExpirationWarningPeriod / 2)
            {
                status.Urgency = RotationUrgency.High;
                status.RequiresRotation = true;
            }
            else if (timeUntilExpiration <= _options.ExpirationWarningPeriod)
            {
                status.Urgency = RotationUrgency.Medium;
                status.RequiresRotation = false;
            }
            else if (timeUntilExpiration <= _options.ExpirationWarningPeriod * 2)
            {
                status.Urgency = RotationUrgency.Low;
                status.RequiresRotation = false;
            }
            else
            {
                status.Urgency = RotationUrgency.None;
                status.RequiresRotation = false;
            }

            statuses.Add(status);
        }

        return statuses.OrderByDescending(s => s.Urgency).ThenBy(s => s.TimeUntilExpiration);
    }

    /// <summary>
    /// Automatically rotates all keys that require rotation based on configuration
    /// </summary>
    public async Task<int> AutoRotateExpiredKeysAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.EnableAutomaticRotation)
        {
            _logger.LogDebug("Automatic rotation is disabled");
            return 0;
        }

        _logger.LogInformation("Starting automatic API key rotation check");

        var now = DateTime.UtcNow;
        var expirationThreshold = now.Subtract(_options.MaxKeyLifetime);

        // Find all active keys older than MaxKeyLifetime
        var expiredKeys = await _apiKeyRepository.FindAsync(
            k => k.IsActive && 
                 k.CreatedAt < expirationThreshold &&
                 !k.Name.Contains("Grace period until"), // Don't rotate keys already in grace period
            cancellationToken);

        var rotatedCount = 0;

        foreach (var key in expiredKeys)
        {
            try
            {
                _logger.LogInformation(
                    "Auto-rotating expired API key {KeyId} ({KeyName}) for tenant {TenantId}",
                    key.Id, key.Name, key.TenantId);

                var result = await RotateApiKeyAsync(key.Id, cancellationToken);
                
                if (result.Success)
                {
                    rotatedCount++;
                    _logger.LogInformation(
                        "Successfully auto-rotated API key {KeyId}. New key: {NewKeyId}",
                        key.Id, result.NewKeyId);
                }
                else
                {
                    _logger.LogWarning(
                        "Failed to auto-rotate API key {KeyId}: {Message}",
                        key.Id, result.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error auto-rotating API key {KeyId}", key.Id);
            }
        }

        _logger.LogInformation("Automatic rotation completed. Rotated {Count} keys", rotatedCount);
        return rotatedCount;
    }

    /// <summary>
    /// Revokes old API keys that have exceeded their grace period
    /// </summary>
    public async Task<int> RevokeExpiredGracePeriodKeysAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.AutoRevokeAfterGracePeriod)
        {
            _logger.LogDebug("Auto-revoke after grace period is disabled");
            return 0;
        }

        _logger.LogInformation("Starting grace period expiration check");

        var now = DateTime.UtcNow;
        var revokedCount = 0;

        // Find all active keys with grace period in their name
        var keysInGracePeriod = await _apiKeyRepository.FindAsync(
            k => k.IsActive && k.Name.Contains("Grace period until"),
            cancellationToken);

        foreach (var key in keysInGracePeriod)
        {
            try
            {
                // Extract grace period end date from name
                var match = System.Text.RegularExpressions.Regex.Match(
                    key.Name, 
                    @"Grace period until (\d{4}-\d{2}-\d{2})");
                
                if (match.Success && DateTime.TryParse(match.Groups[1].Value, out var endDate))
                {
                    if (now > endDate.AddDays(1)) // Add 1 day buffer for end of day
                    {
                        _logger.LogInformation(
                            "Grace period expired for API key {KeyId}. Revoking...",
                            key.Id);

                        await _apiKeyService.RevokeApiKeyAsync(key.Id, cancellationToken);
                        revokedCount++;

                        _logger.LogInformation(
                            "Successfully revoked API key {KeyId} after grace period",
                            key.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking expired grace period key {KeyId}", key.Id);
            }
        }

        _logger.LogInformation(
            "Grace period check completed. Revoked {Count} expired keys",
            revokedCount);
        
        return revokedCount;
    }

    /// <summary>
    /// Validates that a tenant has at least one valid API key and sends warnings if needed
    /// </summary>
    public async Task<bool> ValidateTenantKeysAsync(
        Guid tenantId, 
        CancellationToken cancellationToken = default)
    {
        var statuses = await GetRotationStatusAsync(tenantId, cancellationToken);
        var statusList = statuses.ToList();

        if (!statusList.Any())
        {
            _logger.LogWarning("Tenant {TenantId} has no active API keys", tenantId);
            return false;
        }

        var criticalKeys = statusList.Where(s => s.Urgency == RotationUrgency.Critical).ToList();
        var highUrgencyKeys = statusList.Where(s => s.Urgency == RotationUrgency.High).ToList();

        if (criticalKeys.Any())
        {
            _logger.LogWarning(
                "Tenant {TenantId} has {Count} API keys that have exceeded maximum lifetime",
                tenantId, criticalKeys.Count);
        }

        if (highUrgencyKeys.Any())
        {
            _logger.LogWarning(
                "Tenant {TenantId} has {Count} API keys that should be rotated soon",
                tenantId, highUrgencyKeys.Count);
        }

        return true;
    }
}
