using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using HookVerse.Core.Entities;
using HookVerse.Core.Interfaces;

namespace HookVerse.Infrastructure.Services;

/// <summary>
/// Configuration options for encryption key rotation
/// </summary>
public class EncryptionKeyRotationOptions
{
    /// <summary>
    /// Maximum lifetime of an encryption key before rotation (default: 180 days)
    /// </summary>
    public TimeSpan MaxKeyLifetime { get; set; } = TimeSpan.FromDays(180);

    /// <summary>
    /// Batch size for re-encryption operations (default: 100)
    /// </summary>
    public int ReEncryptionBatchSize { get; set; } = 100;

    /// <summary>
    /// Whether to automatically re-encrypt data during rotation
    /// </summary>
    public bool AutoReEncryptOnRotation { get; set; } = true;

    /// <summary>
    /// Maximum time to spend on re-encryption per batch (default: 30 seconds)
    /// </summary>
    public TimeSpan ReEncryptionBatchTimeout { get; set; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// Result of encryption key rotation
/// </summary>
public class EncryptionKeyRotationResult
{
    public bool Success { get; set; }
    public int NewKeyVersion { get; set; }
    public int TotalRecordsNeedingReEncryption { get; set; }
    public int RecordsReEncrypted { get; set; }
    public string? Message { get; set; }
    public List<string> Warnings { get; set; } = new();

    public static EncryptionKeyRotationResult Successful(
        int newKeyVersion,
        int totalRecords,
        int reEncrypted)
    {
        return new EncryptionKeyRotationResult
        {
            Success = true,
            NewKeyVersion = newKeyVersion,
            TotalRecordsNeedingReEncryption = totalRecords,
            RecordsReEncrypted = reEncrypted,
            Message = "Encryption key rotation completed successfully"
        };
    }

    public static EncryptionKeyRotationResult Failed(string message)
    {
        return new EncryptionKeyRotationResult
        {
            Success = false,
            Message = message
        };
    }
}

/// <summary>
/// Statistics about encrypted data
/// </summary>
public class EncryptionStatistics
{
    public int TotalWebhookEvents { get; set; }
    public int TotalSubscriptions { get; set; }
    public Dictionary<int, int> WebhookEventsByKeyVersion { get; set; } = new();
    public Dictionary<int, int> SubscriptionsByKeyVersion { get; set; } = new();
    public int CurrentKeyVersion { get; set; }
    public int RecordsNeedingReEncryption { get; set; }
    public double ReEncryptionProgress { get; set; }
}

/// <summary>
/// Service for managing encryption key rotation and re-encryption of data
/// </summary>
public class EncryptionKeyRotationService
{
    private readonly IRepository<WebhookEvent> _webhookEventRepository;
    private readonly IRepository<Subscription> _subscriptionRepository;
    private readonly EncryptionService _encryptionService;
    private readonly ILogger<EncryptionKeyRotationService> _logger;
    private readonly EncryptionKeyRotationOptions _options;

    public EncryptionKeyRotationService(
        IRepository<WebhookEvent> webhookEventRepository,
        IRepository<Subscription> subscriptionRepository,
        EncryptionService encryptionService,
        ILogger<EncryptionKeyRotationService> logger,
        IOptions<EncryptionKeyRotationOptions> options)
    {
        _webhookEventRepository = webhookEventRepository ?? throw new ArgumentNullException(nameof(webhookEventRepository));
        _subscriptionRepository = subscriptionRepository ?? throw new ArgumentNullException(nameof(subscriptionRepository));
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Get statistics about encrypted data and key versions in use
    /// </summary>
    public async Task<EncryptionStatistics> GetEncryptionStatisticsAsync(
        CancellationToken cancellationToken = default)
    {
        var stats = new EncryptionStatistics
        {
            CurrentKeyVersion = GetCurrentKeyVersion()
        };

        try
        {
            // Get all webhook events
            var webhookEvents = await _webhookEventRepository.GetAllAsync(cancellationToken);
            stats.TotalWebhookEvents = webhookEvents.Count();

            foreach (var evt in webhookEvents)
            {
                if (!string.IsNullOrEmpty(evt.Payload))
                {
                    var version = _encryptionService.GetKeyVersion(evt.Payload);
                    if (!stats.WebhookEventsByKeyVersion.ContainsKey(version))
                    {
                        stats.WebhookEventsByKeyVersion[version] = 0;
                    }
                    stats.WebhookEventsByKeyVersion[version]++;
                }
            }

            // Get all subscriptions
            var subscriptions = await _subscriptionRepository.GetAllAsync(cancellationToken);
            stats.TotalSubscriptions = subscriptions.Count();

            foreach (var sub in subscriptions)
            {
                if (!string.IsNullOrEmpty(sub.Secret))
                {
                    var version = _encryptionService.GetKeyVersion(sub.Secret);
                    if (!stats.SubscriptionsByKeyVersion.ContainsKey(version))
                    {
                        stats.SubscriptionsByKeyVersion[version] = 0;
                    }
                    stats.SubscriptionsByKeyVersion[version]++;
                }
            }

            // Calculate records needing re-encryption
            stats.RecordsNeedingReEncryption = 
                stats.WebhookEventsByKeyVersion.Where(kvp => kvp.Key != stats.CurrentKeyVersion).Sum(kvp => kvp.Value) +
                stats.SubscriptionsByKeyVersion.Where(kvp => kvp.Key != stats.CurrentKeyVersion).Sum(kvp => kvp.Value);

            var totalRecords = stats.TotalWebhookEvents + stats.TotalSubscriptions;
            if (totalRecords > 0)
            {
                stats.ReEncryptionProgress = 
                    ((double)(totalRecords - stats.RecordsNeedingReEncryption) / totalRecords) * 100;
            }

            _logger.LogInformation(
                "Encryption statistics: {TotalEvents} events, {TotalSubs} subscriptions, " +
                "{NeedingReEncryption} needing re-encryption ({Progress:F2}% complete)",
                stats.TotalWebhookEvents,
                stats.TotalSubscriptions,
                stats.RecordsNeedingReEncryption,
                stats.ReEncryptionProgress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting encryption statistics");
        }

        return stats;
    }

    /// <summary>
    /// Re-encrypt webhook events using the current encryption key
    /// </summary>
    public async Task<int> ReEncryptWebhookEventsAsync(
        int? maxRecords = null,
        CancellationToken cancellationToken = default)
    {
        var batchSize = _options.ReEncryptionBatchSize;
        var reEncryptedCount = 0;
        var currentKeyVersion = GetCurrentKeyVersion();

        _logger.LogInformation(
            "Starting webhook event re-encryption. Target key version: {Version}",
            currentKeyVersion);

        try
        {
            // Get events that need re-encryption
            var allEvents = await _webhookEventRepository.GetAllAsync(cancellationToken);
            var eventsNeedingReEncryption = allEvents
                .Where(e => !string.IsNullOrEmpty(e.Payload) && 
                           _encryptionService.NeedsReEncryption(e.Payload))
                .Take(maxRecords ?? int.MaxValue)
                .ToList();

            _logger.LogInformation(
                "Found {Count} webhook events needing re-encryption",
                eventsNeedingReEncryption.Count);

            foreach (var evt in eventsNeedingReEncryption)
            {
                try
                {
                    var oldVersion = _encryptionService.GetKeyVersion(evt.Payload);
                    evt.Payload = _encryptionService.ReEncrypt(evt.Payload);
                    
                    await _webhookEventRepository.UpdateAsync(evt, cancellationToken);
                    reEncryptedCount++;

                    if (reEncryptedCount % batchSize == 0)
                    {
                        await _webhookEventRepository.SaveChangesAsync(cancellationToken);
                        
                        _logger.LogInformation(
                            "Re-encrypted {Count} webhook events (batch commit)",
                            reEncryptedCount);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error re-encrypting webhook event {EventId}",
                        evt.Id);
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Re-encryption cancelled by user");
                    break;
                }
            }

            // Final save
            if (reEncryptedCount % batchSize != 0)
            {
                await _webhookEventRepository.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation(
                "Completed webhook event re-encryption. Re-encrypted: {Count}",
                reEncryptedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during webhook event re-encryption");
            throw;
        }

        return reEncryptedCount;
    }

    /// <summary>
    /// Re-encrypt subscriptions using the current encryption key
    /// </summary>
    public async Task<int> ReEncryptSubscriptionsAsync(
        int? maxRecords = null,
        CancellationToken cancellationToken = default)
    {
        var batchSize = _options.ReEncryptionBatchSize;
        var reEncryptedCount = 0;
        var currentKeyVersion = GetCurrentKeyVersion();

        _logger.LogInformation(
            "Starting subscription re-encryption. Target key version: {Version}",
            currentKeyVersion);

        try
        {
            // Get subscriptions that need re-encryption
            var allSubscriptions = await _subscriptionRepository.GetAllAsync(cancellationToken);
            var subsNeedingReEncryption = allSubscriptions
                .Where(s => !string.IsNullOrEmpty(s.Secret) && 
                           _encryptionService.NeedsReEncryption(s.Secret))
                .Take(maxRecords ?? int.MaxValue)
                .ToList();

            _logger.LogInformation(
                "Found {Count} subscriptions needing re-encryption",
                subsNeedingReEncryption.Count);

            foreach (var sub in subsNeedingReEncryption)
            {
                try
                {
                    var oldVersion = _encryptionService.GetKeyVersion(sub.Secret);
                    sub.Secret = _encryptionService.ReEncrypt(sub.Secret);
                    
                    // Re-encrypt custom headers if present
                    if (!string.IsNullOrEmpty(sub.CustomHeaders))
                    {
                        sub.CustomHeaders = _encryptionService.ReEncrypt(sub.CustomHeaders);
                    }

                    // Re-encrypt auth config if present
                    if (!string.IsNullOrEmpty(sub.AuthConfig))
                    {
                        sub.AuthConfig = _encryptionService.ReEncrypt(sub.AuthConfig);
                    }
                    
                    await _subscriptionRepository.UpdateAsync(sub, cancellationToken);
                    reEncryptedCount++;

                    if (reEncryptedCount % batchSize == 0)
                    {
                        await _subscriptionRepository.SaveChangesAsync(cancellationToken);
                        
                        _logger.LogInformation(
                            "Re-encrypted {Count} subscriptions (batch commit)",
                            reEncryptedCount);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error re-encrypting subscription {SubscriptionId}",
                        sub.Id);
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Re-encryption cancelled by user");
                    break;
                }
            }

            // Final save
            if (reEncryptedCount % batchSize != 0)
            {
                await _subscriptionRepository.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation(
                "Completed subscription re-encryption. Re-encrypted: {Count}",
                reEncryptedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during subscription re-encryption");
            throw;
        }

        return reEncryptedCount;
    }

    /// <summary>
    /// Perform complete key rotation: update configuration and re-encrypt all data
    /// </summary>
    /// <remarks>
    /// This method assumes the new encryption key has been deployed to the environment
    /// and is available via configuration update (Kubernetes secret, environment variable, etc.)
    /// </remarks>
    public async Task<EncryptionKeyRotationResult> PerformKeyRotationAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting encryption key rotation");

        try
        {
            // Get statistics before rotation
            var statsBefore = await GetEncryptionStatisticsAsync(cancellationToken);
            var currentVersion = GetCurrentKeyVersion();

            _logger.LogInformation(
                "Current key version: {Version}. Records needing re-encryption: {Count}",
                currentVersion,
                statsBefore.RecordsNeedingReEncryption);

            if (statsBefore.RecordsNeedingReEncryption == 0)
            {
                return new EncryptionKeyRotationResult
                {
                    Success = true,
                    NewKeyVersion = currentVersion,
                    TotalRecordsNeedingReEncryption = 0,
                    RecordsReEncrypted = 0,
                    Message = "All data is already encrypted with the current key version"
                };
            }

            var totalReEncrypted = 0;

            // Re-encrypt webhook events
            if (_options.AutoReEncryptOnRotation)
            {
                _logger.LogInformation("Re-encrypting webhook events...");
                var eventsReEncrypted = await ReEncryptWebhookEventsAsync(null, cancellationToken);
                totalReEncrypted += eventsReEncrypted;

                _logger.LogInformation("Re-encrypting subscriptions...");
                var subsReEncrypted = await ReEncryptSubscriptionsAsync(null, cancellationToken);
                totalReEncrypted += subsReEncrypted;
            }

            // Get statistics after rotation
            var statsAfter = await GetEncryptionStatisticsAsync(cancellationToken);

            var result = EncryptionKeyRotationResult.Successful(
                currentVersion,
                statsBefore.RecordsNeedingReEncryption,
                totalReEncrypted);

            if (statsAfter.RecordsNeedingReEncryption > 0)
            {
                result.Warnings.Add(
                    $"{statsAfter.RecordsNeedingReEncryption} records still need re-encryption. " +
                    "Run re-encryption again or check for errors.");
            }

            _logger.LogInformation(
                "Encryption key rotation completed. Re-encrypted {Count} records",
                totalReEncrypted);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during encryption key rotation");
            return EncryptionKeyRotationResult.Failed($"Key rotation failed: {ex.Message}");
        }
    }

    private int GetCurrentKeyVersion()
    {
        // This would typically come from the EncryptionService or configuration
        // For now, we'll use a placeholder
        return 1;
    }
}
