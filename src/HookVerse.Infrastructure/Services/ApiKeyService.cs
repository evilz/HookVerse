using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using HookVerse.Core.Entities;
using HookVerse.Core.Interfaces;

namespace HookVerse.Infrastructure.Services;

/// <summary>
/// Implementation of API key service
/// </summary>
public class ApiKeyService : IApiKeyService
{
    private readonly IRepository<ApiKey> _apiKeyRepository;
    private readonly ILogger<ApiKeyService> _logger;
    private const int ApiKeyLength = 32; // bytes
    private const int KeyPrefixLength = 8; // characters to show

    public ApiKeyService(
        IRepository<ApiKey> apiKeyRepository,
        ILogger<ApiKeyService> logger)
    {
        _apiKeyRepository = apiKeyRepository ?? throw new ArgumentNullException(nameof(apiKeyRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ApiKeyValidationResult> ValidateApiKeyAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return ApiKeyValidationResult.Failure("API key is required");
        }

        try
        {
            // Hash the provided API key
            var hashedKey = HashApiKey(apiKey);

            // Find the API key in database
            var key = await _apiKeyRepository.FirstOrDefaultAsync(
                k => k.HashedKey == hashedKey && k.IsActive,
                cancellationToken);

            if (key == null)
            {
                _logger.LogWarning("Invalid API key attempt");
                return ApiKeyValidationResult.Failure("Invalid API key");
            }

            // Update last used timestamp
            key.LastUsedAt = DateTime.UtcNow;
            await _apiKeyRepository.UpdateAsync(key, cancellationToken);
            await _apiKeyRepository.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("API key validated for tenant {TenantId}", key.TenantId);

            return ApiKeyValidationResult.Success(key.TenantId, key.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating API key");
            return ApiKeyValidationResult.Failure("Error validating API key");
        }
    }

    public async Task<string> GenerateApiKeyAsync(Guid tenantId, string name, CancellationToken cancellationToken = default)
    {
        // Generate a cryptographically secure random API key
        var keyBytes = new byte[ApiKeyLength];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(keyBytes);
        }

        var apiKey = Convert.ToBase64String(keyBytes)
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "")
            .Substring(0, 40); // 40 character API key

        var hashedKey = HashApiKey(apiKey);
        var keyPrefix = apiKey.Substring(0, KeyPrefixLength);

        var apiKeyEntity = new ApiKey
        {
            TenantId = tenantId,
            Name = name,
            HashedKey = hashedKey,
            KeyPrefix = keyPrefix,
            IsActive = true
        };

        await _apiKeyRepository.AddAsync(apiKeyEntity, cancellationToken);
        await _apiKeyRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Generated new API key {KeyId} for tenant {TenantId}", apiKeyEntity.Id, tenantId);

        // Return the plain-text key (only time it's visible)
        return apiKey;
    }

    public async Task RevokeApiKeyAsync(Guid apiKeyId, CancellationToken cancellationToken = default)
    {
        var apiKey = await _apiKeyRepository.GetByIdAsync(apiKeyId, cancellationToken);
        
        if (apiKey == null)
        {
            throw new InvalidOperationException($"API key {apiKeyId} not found");
        }

        apiKey.IsActive = false;
        apiKey.RevokedAt = DateTime.UtcNow;

        await _apiKeyRepository.UpdateAsync(apiKey, cancellationToken);
        await _apiKeyRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Revoked API key {KeyId}", apiKeyId);
    }

    public async Task<IEnumerable<ApiKeyInfo>> GetApiKeysAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var keys = await _apiKeyRepository.FindAsync(
            k => k.TenantId == tenantId,
            cancellationToken);

        return keys.Select(k => new ApiKeyInfo
        {
            Id = k.Id,
            Name = k.Name,
            CreatedAt = k.CreatedAt,
            LastUsedAt = k.LastUsedAt,
            IsActive = k.IsActive,
            KeyPrefix = k.KeyPrefix
        });
    }

    private static string HashApiKey(string apiKey)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToBase64String(hashBytes);
    }
}
